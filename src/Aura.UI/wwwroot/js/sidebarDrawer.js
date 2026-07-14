/**
 * Sidebar drawer module — handles hamburger toggle, backdrop dismiss,
 * Escape key, nav-link auto-close, and body scroll lock.
 * Pure DOM manipulation — no Blazor state involved.
 */
window.sidebarDrawer = (function () {
    let _escapeHandler = null;
    let _navLinkHandlers = [];

    function _getSidebar() {
        return document.querySelector('.dashboard-sidebar');
    }

    function _getBackdrop() {
        return document.querySelector('.backdrop');
    }

    function _isOpen() {
        const sidebar = _getSidebar();
        return sidebar && sidebar.classList.contains('dashboard-sidebar--open');
    }

    function _attachNavLinkListeners() {
        const navLinks = document.querySelectorAll('.dashboard-sidebar__nav-item');
        navLinks.forEach(function (link) {
            const handler = function () {
                closeDrawer();
            };
            link.addEventListener('click', handler);
            _navLinkHandlers.push({ element: link, handler: handler });
        });
    }

    function _detachNavLinkListeners() {
        _navLinkHandlers.forEach(function (entry) {
            entry.element.removeEventListener('click', entry.handler);
        });
        _navLinkHandlers = [];
    }

    function init() {
        // Attach Escape key listener
        _escapeHandler = function (e) {
            if (e.key === 'Escape' && _isOpen()) {
                closeDrawer();
            }
        };
        document.addEventListener('keydown', _escapeHandler);

        // Attach nav-link click listeners
        _attachNavLinkListeners();
    }

    function toggleDrawer() {
        const sidebar = _getSidebar();
        const backdrop = _getBackdrop();
        if (!sidebar) return;

        const opening = !sidebar.classList.contains('dashboard-sidebar--open');

        sidebar.classList.toggle('dashboard-sidebar--open');

        if (backdrop) {
            backdrop.classList.toggle('backdrop--visible');
        }

        // Body scroll lock
        document.body.style.overflow = opening ? 'hidden' : '';
    }

    function closeDrawer() {
        const sidebar = _getSidebar();
        const backdrop = _getBackdrop();
        if (!sidebar) return;

        sidebar.classList.remove('dashboard-sidebar--open');

        if (backdrop) {
            backdrop.classList.remove('backdrop--visible');
        }

        // Release body scroll lock
        document.body.style.overflow = '';
    }

    function dispose() {
        if (_escapeHandler) {
            document.removeEventListener('keydown', _escapeHandler);
            _escapeHandler = null;
        }
        _detachNavLinkListeners();
        closeDrawer();
    }

    return {
        init: init,
        toggleDrawer: toggleDrawer,
        closeDrawer: closeDrawer,
        dispose: dispose
    };
})();
