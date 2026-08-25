using Aiursoft.MusicExam.Services.FileStorage;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class MediaDeliverySecurityTests : TestBase
{
    [TestMethod]
    public async Task RecognizedAudioAndVideoAreServedInline()
    {
        await LoginAsAdmin();

        await AssertInline(CreateWave(), "sample.wav", "audio/wav");
        await AssertInline(CreateMp4(), "sample.mp4", "video/mp4");
    }

    [TestMethod]
    public async Task ForgedAudioIsServedAsAnAttachment()
    {
        await LoginAsAdmin();
        var response = await UploadAndDownload(
            "<script>alert(document.domain)</script>"u8.ToArray(),
            "attack.mp3");

        Assert.AreEqual("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.AreEqual("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    private async Task AssertInline(byte[] content, string fileName, string contentType)
    {
        var response = await UploadAndDownload(content, fileName);

        Assert.AreEqual(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("inline", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.AreEqual("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    private async Task<HttpResponseMessage> UploadAndDownload(byte[] content, string fileName)
    {
        var storage = GetService<StorageService>();
        var request = new MultipartFormDataContent
        {
            { new ByteArrayContent(content), "file", fileName }
        };
        var uploadResponse = await Http.PostAsync(
            storage.GetUploadUrl("questions", maxSizeInMb: 1, allowedExtensions: "mp3 mp4 wav"),
            request);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();

        var downloadResponse = await Http.GetAsync($"/download/{upload!.Path}");
        downloadResponse.EnsureSuccessStatusCode();
        return downloadResponse;
    }

    private static byte[] CreateWave()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(37);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(8000);
        writer.Write(8000);
        writer.Write((short)1);
        writer.Write((short)8);
        writer.Write("data"u8);
        writer.Write(1);
        writer.Write((byte)128);
        return stream.ToArray();
    }

    private static byte[] CreateMp4()
    {
        return
        [
            0, 0, 0, 24, (byte)'f', (byte)'t', (byte)'y', (byte)'p',
            (byte)'i', (byte)'s', (byte)'o', (byte)'m', 0, 0, 2, 0,
            (byte)'i', (byte)'s', (byte)'o', (byte)'m', (byte)'m', (byte)'p', (byte)'4', (byte)'2'
        ];
    }

    private sealed class UploadResult
    {
        public string Path { get; init; } = string.Empty;
    }
}
