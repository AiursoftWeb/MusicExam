using Aiursoft.Scanner.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Aiursoft.MusicExam.Services.FileStorage;

public enum FilePermission
{
    Upload,
    Download
}

/// <summary>
/// Represents a service for storing and managing files. (Level 3: Business Gateway)
/// </summary>
public class StorageService(
    FeatureFoldersProvider folders,
    FileLockProvider fileLockProvider,
    IDataProtectionProvider dataProtectionProvider) : ITransientDependency
{
    /// <summary>
    /// Saves a file to the storage.
    /// </summary>
    /// <param name="logicalPath">The logical path (relative to Workspace) where the file will be saved.</param>
    /// <param name="file">The file to be saved.</param>
    /// <param name="isVault">Whether to save to the private Vault.</param>
    /// <returns>The actual logical path where the file is saved (may differ if renamed).</returns>
    public async Task<string> Save(string logicalPath, IFormFile file, bool isVault = false)
    {
        // 1. Get Workspace root
        var root = isVault ? folders.GetVaultFolder() : folders.GetWorkspaceFolder();

        // 2. Resolve physical path
        var physicalPath = Path.GetFullPath(Path.Combine(root, logicalPath));

        // 3. Security check: Ensure path is within Workspace
        if (!physicalPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Path traversal attempt detected!");
        }

        // 4. Create directory if needed
        var directory = Path.GetDirectoryName(physicalPath);
        if (File.Exists(directory))
        {
            File.Delete(directory);
        }
        if (!Directory.Exists(directory))
        {
             Directory.CreateDirectory(directory!);
        }

        // 5. Handle collisions (Renaming)
        // Lock on the directory to prevent race conditions during renaming
        var lockObj = fileLockProvider.GetLock(directory!);
        await lockObj.WaitAsync();
        try
        {
            var expectedFileName = Path.GetFileName(physicalPath);
            while (File.Exists(physicalPath))
            {
                expectedFileName = "_" + expectedFileName;
                physicalPath = Path.Combine(directory!, expectedFileName);
            }

            // Create placeholder to reserve name
            File.Create(physicalPath).Close();
        }
        finally
        {
            lockObj.Release();
        }

        // 6. Write file content
        await using var fileStream = new FileStream(physicalPath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        // 7. Return logical path (relative to Workspace)
        return Path.GetRelativePath(root, physicalPath).Replace("\\", "/");
    }

    /// <summary>
    /// Saves a file from a physical path to the storage.
    /// </summary>
    /// <param name="sourcePhysicalPath">The physical path of the source file to copy.</param>
    /// <param name="destinationLogicalPath">The logical path (relative to Workspace/Vault) where the file will be saved.</param>
    /// <param name="isVault">Whether to save to the private Vault.</param>
    /// <returns>The actual logical path where the file is saved (may differ if renamed).</returns>
    public async Task<string> SaveFileFromPhysicalPath(string sourcePhysicalPath, string destinationLogicalPath, bool isVault = false)
    {
        // 1. Validate source file exists
        if (!File.Exists(sourcePhysicalPath))
        {
            throw new FileNotFoundException($"Source file not found: {sourcePhysicalPath}");
        }

        // 2. Get destination Workspace root
        var root = isVault ? folders.GetVaultFolder() : folders.GetWorkspaceFolder();

        // 3. Resolve physical destination path
        var physicalPath = Path.GetFullPath(Path.Combine(root, destinationLogicalPath));

        // 4. Security check: Ensure path is within Workspace
        if (!physicalPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Path traversal attempt detected!");
        }

        // 5. Create directory if needed
        var directory = Path.GetDirectoryName(physicalPath);
        if (!Directory.Exists(directory))
        {
             Directory.CreateDirectory(directory!);
        }

        // 6. Handle collisions (Renaming)
        // Lock on the directory to prevent race conditions during renaming
        var lockObj = fileLockProvider.GetLock(directory!);
        await lockObj.WaitAsync();
        try
        {
            var expectedFileName = Path.GetFileName(physicalPath);
            while (File.Exists(physicalPath))
            {
                expectedFileName = "_" + expectedFileName;
                physicalPath = Path.Combine(directory!, expectedFileName);
            }

            // Create placeholder to reserve name
            File.Create(physicalPath).Close();
        }
        finally
        {
            lockObj.Release();
        }

        // 7. Copy file content
        await using var sourceStream = new FileStream(sourcePhysicalPath, FileMode.Open, FileAccess.Read);
        await using var destinationStream = new FileStream(physicalPath, FileMode.Create);
        await sourceStream.CopyToAsync(destinationStream);

        // 8. Return logical path (relative to Workspace)
        return Path.GetRelativePath(root, physicalPath).Replace("\\", "/");
    }

    /// <summary>
    /// Retrieves the physical file path for a given logical path.
    /// Defaults to Workspace.
    /// </summary>
    public string GetFilePhysicalPath(string logicalPath, bool isVault = false)
    {
        var root = isVault ? folders.GetVaultFolder() : folders.GetWorkspaceFolder();
        var physicalPath = Path.GetFullPath(Path.Combine(root, logicalPath));

        if (!physicalPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Restricted path access!");
        }
        return physicalPath;
    }

    public string GetToken(string path, FilePermission permission)
    {
        // Create a time-limited data protector with 60-minute expiration
        var protector = dataProtectionProvider
            .CreateProtector("FileOperation")
            .ToTimeLimitedDataProtector();

        var tokenData = $"{path}|{permission}";

        // Protect the path with time-limited encryption
        var protectedData = protector.Protect(tokenData, TimeSpan.FromMinutes(60));
        return protectedData;
    }

    public bool ValidateToken(string requestPath, string tokenString, FilePermission requiredPermission)
    {
        try
        {
            // Create the same protector used for token generation
            var protector = dataProtectionProvider
                .CreateProtector("FileOperation")
                .ToTimeLimitedDataProtector();

            // Unprotect and validate expiration automatically
            var tokenData = protector.Unprotect(tokenString);
            var parts = tokenData.Split('|');
            if (parts.Length != 2) return false;

            var authorizedPath = parts[0];
            var authorizedPermission = Enum.Parse<FilePermission>(parts[1]);

            if (authorizedPermission != requiredPermission) return false;

            // Verify the token authorizes access to the requested path
            return requestPath.StartsWith(authorizedPath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Token is invalid, expired, or tampered with
            return false;
        }
    }

    /// <summary>
    /// Converts a logical path to a URI-compatible path.
    /// </summary>
    private string RelativePathToUriPath(string relativePath)
    {
        var urlPath = Uri.EscapeDataString(relativePath)
            .Replace("%5C", "/")
            .Replace("%5c", "/")
            .Replace("%2F", "/")
            .Replace("%2f", "/")
            .TrimStart('/');
        return urlPath;
    }

    public string RelativePathToInternetUrl(string relativePath, HttpContext context, bool isVault)
    {
        if (isVault)
        {
            var token = GetToken(relativePath, FilePermission.Download);
            return $"{context.Request.Scheme}://{context.Request.Host}/download-private/{RelativePathToUriPath(relativePath)}?token={token}";
        }
        return $"{context.Request.Scheme}://{context.Request.Host}/download/{RelativePathToUriPath(relativePath)}";
    }

    public string RelativePathToInternetUrl(string relativePath, bool isVault)
    {
        if (isVault)
        {
            var token = GetToken(relativePath, FilePermission.Download);
            return $"/download-private/{RelativePathToUriPath(relativePath)}?token={token}";
        }
        return $"/download/{RelativePathToUriPath(relativePath)}";
    }

    public string GetUploadUrl(string subfolder, bool isVault = false)
    {
        if (isVault)
        {
            var token = GetToken(subfolder, FilePermission.Upload);
            return $"/upload-private/{subfolder}?token={token}";
        }
        return $"/upload/{subfolder}";
    }
}
