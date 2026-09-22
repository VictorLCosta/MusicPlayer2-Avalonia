export function create(onEnded) {
    const audio = new Audio();
    audio.preload = "metadata";
    audio.addEventListener("ended", onEnded);

    return audio;
}

export function load(audio, source) {
    return new Promise((resolve, reject) => {
        audio.pause();
        audio.src = source;
        audio.currentTime = 0;

        audio.onloadedmetadata = () => resolve();
        audio.onerror = () => reject(new Error("Nao foi possivel carregar o audio."));

        audio.load();
    });
}

export function play(audio) {
    audio.play().catch(error => console.warn("Reproducao bloqueada:", error));
}

export function pause(audio) {
    audio.pause();
}

export function stop(audio) {
    audio.pause();
    audio.currentTime = 0;
}

export function isPlaying(audio) {
    return !audio.paused && !audio.ended;
}

export function getCurrentTime(audio) {
    return audio.currentTime;
}

export function setCurrentTime(audio, seconds) {
    audio.currentTime = seconds;
}

export function getDuration(audio) {
    return audio.duration;
}

export function getVolume(audio) {
    return audio.volume;
}

export function setVolume(audio, volume) {
    audio.volume = volume;
}

export function destroy(audio) {
    audio.pause();
    audio.removeAttribute("src");
    audio.load();
}