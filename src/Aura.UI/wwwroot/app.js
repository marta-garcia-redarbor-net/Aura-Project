window.showToast = function (message, duration) {
    duration = duration || 4000;

    var toast = document.createElement('div');
    toast.textContent = message;
    toast.style.cssText =
        'position:fixed;bottom:24px;left:50%;transform:translateX(-50%);' +
        'z-index:9999;background:#1E293B;color:#DAE2FD;' +
        'border:1px solid #334155;border-radius:8px;' +
        'padding:12px 20px;font-family:Inter,system-ui,sans-serif;' +
        'font-size:14px;box-shadow:0 4px 16px rgba(0,0,0,0.4);' +
        'opacity:0;transition:opacity 0.3s ease;' +
        'max-width:90vw;text-align:center;';

    document.body.appendChild(toast);

    requestAnimationFrame(function () {
        toast.style.opacity = '1';
    });

    setTimeout(function () {
        toast.style.opacity = '0';
        setTimeout(function () {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    }, duration);
};
