/**
 * Work Item Alert JS Interop
 * Handles browser notifications and audio playback for urgent work item alerts.
 * Follows the same pattern as meetingAlert.js for consistency.
 */
window.workItemAlert = {
    _audioPrimed: false,
    _audioElement: null,

    /**
     * Shows a browser notification with work item details.
     * Falls back gracefully if Notification API is not supported or permission is denied.
     * @param {string} title - Work item title
     * @param {string} body - Work item description or priority
     */
    showNotification: function (title, body) {
        if (typeof Notification === 'undefined') {
            console.warn('WorkItemAlert: Notification API not supported');
            return;
        }

        if (Notification.permission === 'denied') {
            console.warn('WorkItemAlert: Notification permission denied');
            return;
        }

        if (Notification.permission === 'default') {
            Notification.requestPermission().then(function (permission) {
                if (permission === 'granted') {
                    window.workItemAlert._createNotification(title, body);
                }
            });
            return;
        }

        window.workItemAlert._createNotification(title, body);
    },

    /**
     * Creates and shows a browser notification.
     * @param {string} title - Notification title
     * @param {string} body - Notification body
     */
    _createNotification: function (title, body) {
        try {
            var notification = new Notification(title, {
                body: body,
                icon: '/favicon.ico',
                tag: 'work-item-alert-' + Date.now()
            });

            setTimeout(function () {
                notification.close();
            }, 10000);
        } catch (error) {
            console.error('WorkItemAlert: Failed to create notification', error);
        }
    },

    /**
     * Plays the work item alert sound.
     * Audio must be primed first via primeAudio() after user gesture.
     */
    playSound: function () {
        if (!window.workItemAlert._audioPrimed) {
            console.warn('WorkItemAlert: Audio not primed. Call primeAudio() after user gesture.');
            return;
        }

        try {
            if (window.workItemAlert._audioElement) {
                window.workItemAlert._audioElement.currentTime = 0;
                window.workItemAlert._audioElement.play().catch(function (error) {
                    console.error('WorkItemAlert: Failed to play sound', error);
                });
            }
        } catch (error) {
            console.error('WorkItemAlert: Failed to play sound', error);
        }
    },

    /**
     * Primes the audio element for playback.
     * Must be called after a user gesture to satisfy browser autoplay policy.
     */
    primeAudio: function () {
        if (window.workItemAlert._audioPrimed) {
            return;
        }

        try {
            window.workItemAlert._audioElement = new Audio('/work-item-alert.wav');
            window.workItemAlert._audioElement.load();
            window.workItemAlert._audioPrimed = true;
            console.info('WorkItemAlert: Audio primed for playback');
        } catch (error) {
            console.error('WorkItemAlert: Failed to prime audio', error);
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
