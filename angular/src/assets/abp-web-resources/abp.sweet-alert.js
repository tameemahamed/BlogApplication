/* eslint-disable no-undef */
var abp = abp || {};

(function () {
    if (!Swal) {
        return;
    }
    /* MESSAGE **************************************************/

    // Design system (prd.md §6.1): the UI is text-first - no icon fonts and
    // no red/green severity. Dialogs carry meaning through their copy, and
    // colors come from the palette tokens via blog-theme.css (.swal2-popup).
    var showMessage = function showMessage(message, title, isHtml, options) {
        if (!title) {
            title = message;
            message = undefined;
        }

        options = options || {};
        options.title = title;
        options.icon = undefined;
        options.confirmButtonText = options.confirmButtonText || abp.localization.abpWeb('Ok');

        if (isHtml) {
            options.html = message;
        } else {
            options.text = message;
        }

        return Swal.fire(options);
    };

    abp.message.info = function (message, title, isHtml, options) {
        return showMessage(message, title, isHtml, options);
    };

    abp.message.success = function (message, title, isHtml, options) {
        return showMessage(message, title, isHtml, options);
    };

    abp.message.warn = function (message, title, isHtml, options) {
        return showMessage(message, title, isHtml, options);
    };

    abp.message.error = function (message, title, isHtml, options) {
        return showMessage(message, title, isHtml, options);
    };

    abp.message.confirm = function (message, titleOrCallback, callback, isHtml, options) {
        var title = undefined;

        if (typeof titleOrCallback === 'function') {
            callback = titleOrCallback;
        } else if (titleOrCallback) {
            title = titleOrCallback;
        }

        options = options || {};
        options.title = title ? title : abp.localization.abpWeb('AreYouSure');
        options.icon = undefined;
        options.confirmButtonText = options.confirmButtonText || abp.localization.abpWeb('Yes');
        options.cancelButtonText = options.cancelButtonText || abp.localization.abpWeb('Cancel');
        options.showCancelButton = true;

        if (isHtml) {
            options.html = message;
        } else {
            options.text = message;
        }

        return Swal.fire(options).then(function (result) {
            callback && callback(result.value);
        });
    };
    /* NOTIFICATION *********************************************/

    var Toast = Swal.mixin({
        toast: true,
        position: 'bottom-end',
        showConfirmButton: false,
        timer: 3000,
    });

    var showNotification = function showNotification(message, title, options) {
        options = options || {};

        if (title) {
            options.title = title;
        }

        options.html = ''.concat('<span>', message, '</span>');
        Toast.fire(options);
    };

    abp.notify.success = function (message, title, options) {
        showNotification(message, title, options);
    };

    abp.notify.info = function (message, title, options) {
        showNotification(message, title, options);
    };

    abp.notify.warn = function (message, title, options) {
        showNotification(message, title, options);
    };

    abp.notify.error = function (message, title, options) {
        showNotification(message, title, options);
    };
})();
