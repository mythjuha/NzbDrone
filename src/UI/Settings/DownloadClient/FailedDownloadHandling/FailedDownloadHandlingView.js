﻿'use strict';
define(
    [
        'marionette',
        'Mixins/AsModelBoundView',
        'Mixins/AsValidatedView'
    ], function (Marionette, AsModelBoundView, AsValidatedView) {

        var view = Marionette.ItemView.extend({
            template: 'Settings/DownloadClient/FailedDownloadHandling/FailedDownloadHandlingViewTemplate',

            ui: {
                failedDownloadHandlingCheckbox: '.x-failed-download-handling',
                failedDownloadOptions         : '.x-failed-download-options'
            },

            events: {
                'change .x-failed-download-handling': '_setFailedDownloadOptionsVisibility'
            },

            _setFailedDownloadOptionsVisibility: function () {
                var checked = this.ui.failedDownloadHandlingCheckbox.prop('checked');
                if (checked) {
                    this.ui.failedDownloadOptions.slideDown();
                }

                else {
                    this.ui.failedDownloadOptions.slideUp();
                }
            }
        });

        AsModelBoundView.call(view);
        AsValidatedView.call(view);

        return view;
    });
