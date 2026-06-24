/**
 * Meeting Alert JS Interop
 * Handles browser notifications and audio playback for meeting alerts.
 * Provides graceful fallback when Notification API is not supported.
 */
window.meetingAlert = {
    _audioPrimed: false,
    _audioElement: null,

    /**
     * Shows a browser notification with meeting title and time.
     * Falls back gracefully if Notification API is not supported or permission is denied.
     * @param {string} title - Meeting title
     * @param {string} body - Meeting time/description
     */
    showNotification: function (title, body) {
        // Check if Notification API is supported
        if (typeof Notification === 'undefined') {
            console.warn('MeetingAlert: Notification API not supported');
            return;
        }

        // Check permission status
        if (Notification.permission === 'denied') {
            console.warn('MeetingAlert: Notification permission denied');
            return;
        }

        // Request permission if not yet granted (requires user gesture)
        if (Notification.permission === 'default') {
            Notification.requestPermission().then(function (permission) {
                if (permission === 'granted') {
                    window.meetingAlert._createNotification(title, body);
                }
            });
            return;
        }

        // Permission granted - show notification
        window.meetingAlert._createNotification(title, body);
    },

    /**
     * Creates and shows a browser notification.
     * @param {string} title - Meeting title
     * @param {string} body - Meeting time/description
     */
    _createNotification: function (title, body) {
        try {
            var notification = new Notification(title, {
                body: body,
                icon: '/favicon.ico',
                tag: 'meeting-alert-' + Date.now()
            });

            // Auto-close after 10 seconds
            setTimeout(function () {
                notification.close();
            }, 10000);
        } catch (error) {
            console.error('MeetingAlert: Failed to create notification', error);
        }
    },

    /**
     * Plays the meeting alert sound.
     * Audio must be primed first via primeAudio() after user gesture.
     */
    playSound: function () {
        if (!window.meetingAlert._audioPrimed) {
            console.warn('MeetingAlert: Audio not primed. Call primeAudio() after user gesture.');
            return;
        }

        try {
            if (window.meetingAlert._audioElement) {
                window.meetingAlert._audioElement.currentTime = 0;
                window.meetingAlert._audioElement.play().catch(function (error) {
                    console.error('MeetingAlert: Failed to play sound', error);
                });
            }
        } catch (error) {
            console.error('MeetingAlert: Failed to play sound', error);
        }
    },

    /**
     * Primes the audio element for playback.
     * Must be called after a user gesture to satisfy browser autoplay policy.
     */
    primeAudio: function () {
        if (window.meetingAlert._audioPrimed) {
            return;
        }

        try {
            window.meetingAlert._audioElement = new Audio('/meeting-alert.wav');
            window.meetingAlert._audioElement.load();
            window.meetingAlert._audioPrimed = true;
            console.info('MeetingAlert: Audio primed for playback');
        } catch (error) {
            console.error('MeetingAlert: Failed to prime audio', error);
        }
    },

    /**
     * Requests notification permission from the user.
     * Should be called on user gesture.
     * @returns {Promise<string>} Permission status
     */
    requestPermission: function () {
        if (typeof Notification === 'undefined') {
            return Promise.resolve('unsupported');
        }

        if (Notification.permission === 'granted') {
            return Promise.resolve('granted');
        }

        if (Notification.permission === 'denied') {
            return Promise.resolve('denied');
        }

        return Notification.requestPermission();
    }
};