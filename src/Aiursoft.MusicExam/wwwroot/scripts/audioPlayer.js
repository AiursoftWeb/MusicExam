class MusicExamAudioPlayer {
    static activePlayer = null;

    constructor(container, src) {
        this.container = container;
        this.src = src;
        this.audio = new Audio(src);
        this.isPlaying = false;

        this.render();
        this.attachEvents();
    }

    render() {
        this.container.innerHTML = `
            <div class="custom-audio-player d-flex align-items-center">
                <button class="btn btn-sm btn-primary play-pause-btn d-flex align-items-center justify-content-center" style="height: 32px; width: 32px; padding: 0;" type="button">
                    <i class="fas fa-play"></i>
                </button>
                <div class="progress-container mx-2 flex-grow-1">
                    <div class="progress-bar"></div>
                </div>
                <button class="btn btn-sm btn-outline-secondary replay-btn d-flex align-items-center justify-content-center" style="height: 32px; width: 32px; padding: 0;" type="button">
                    <i class="fas fa-redo"></i>
                </button>
                <div class="time-display ms-2 small text-muted text-nowrap">0:00 / 0:00</div>
            </div>
        `;

        this.playPauseBtn = this.container.querySelector('.play-pause-btn');
        this.playPauseIcon = this.playPauseBtn.querySelector('i');
        this.progressBar = this.container.querySelector('.progress-bar');
        this.progressContainer = this.container.querySelector('.progress-container');
        this.replayBtn = this.container.querySelector('.replay-btn');
        this.timeDisplay = this.container.querySelector('.time-display');
    }

    attachEvents() {
        // Play/Pause Toggle
        this.playPauseBtn.addEventListener('click', (e) => {
            e.preventDefault(); // Prevent form submission if inside a form
            this.togglePlay();
        });

        // Replay
        this.replayBtn.addEventListener('click', (e) => {
            e.preventDefault();
            this.replay();
        });

        // Audio Events
        this.audio.addEventListener('timeupdate', () => this.updateProgress());
        this.audio.addEventListener('loadedmetadata', () => this.updateTimeDisplay());
        this.audio.addEventListener('ended', () => {
            this.isPlaying = false;
            this.updateIcon();
            this.audio.currentTime = 0;
            this.updateProgress();
        });

        // Click on progress bar to seek
        this.progressContainer.addEventListener('click', (e) => {
            const width = this.progressContainer.clientWidth;
            const clickX = e.offsetX;
            const duration = this.audio.duration;
            this.audio.currentTime = (clickX / width) * duration;
        });
    }

    togglePlay() {
        if (this.isPlaying) {
            this.pause();
        } else {
            this.play();
        }
    }

    async play() {
        // Stop currently active player if it's not this one
        if (MusicExamAudioPlayer.activePlayer && MusicExamAudioPlayer.activePlayer !== this) {
            MusicExamAudioPlayer.activePlayer.pause();
        }

        try {
            await this.audio.play();
            this.isPlaying = true;
            MusicExamAudioPlayer.activePlayer = this;
            this.updateIcon();
        } catch (err) {
            console.error("Autoplay failed or was blocked:", err);
            this.isPlaying = false;
            this.updateIcon();
        }
    }

    pause() {
        this.audio.pause();
        this.isPlaying = false;
        this.updateIcon();
    }

    replay() {
        this.audio.currentTime = 0;
        this.play();
    }

    updateIcon() {
        if (this.isPlaying) {
            this.playPauseIcon.classList.remove('fa-play');
            this.playPauseIcon.classList.add('fa-pause');
            this.playPauseBtn.classList.replace('btn-primary', 'btn-warning');
        } else {
            this.playPauseIcon.classList.remove('fa-pause');
            this.playPauseIcon.classList.add('fa-play');
            this.playPauseBtn.classList.replace('btn-warning', 'btn-primary');
        }
    }

    updateProgress() {
        const { duration, currentTime } = this.audio;
        if (isNaN(duration)) return;

        const progressPercent = (currentTime / duration) * 100;
        this.progressBar.style.width = `${progressPercent}%`;
        this.updateTimeDisplay();
    }

    updateTimeDisplay() {
        const { duration, currentTime } = this.audio;
        if (isNaN(duration)) {
             this.timeDisplay.innerText = "0:00 / 0:00";
             return;
        }
        
        this.timeDisplay.innerText = `${this.formatTime(currentTime)} / ${this.formatTime(duration)}`;
    }

    formatTime(seconds) {
        const min = Math.floor(seconds / 60);
        const sec = Math.floor(seconds % 60);
        return `${min}:${sec < 10 ? '0' : ''}${sec}`;
    }
}
