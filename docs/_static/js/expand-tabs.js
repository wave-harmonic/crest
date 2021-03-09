
// Taken from: https://stackoverflow.com/a/8747204
jQuery.expr[':'].icontains = function (a, i, m) {
    return jQuery(a).text().toUpperCase()
        .indexOf(m[3].toUpperCase()) >= 0;
};

// Adapted from: https://raw.githubusercontent.com/readthedocs/readthedocs.org/738b6b2836a7e0cadad48e7f407fdeaf7ba7a1d7/docs/_static/js/expand_tabs.js
/*
 * Expands a specific tab of sphinx-tabs.
 * Usage:
 * - docs.readthedocs.io/?tab=Name
 * - docs.readthedocs.io/?tab=Name#section
 * Where 'Name' is the title of the tab (case sensitive).
*/
$(document).ready(function () {
    const urlParams = new URLSearchParams(window.location.search);
    const tabName = urlParams.get('tab');
    if (tabName !== null) {
        const tab = $('.sphinx-tabs-tab:icontains("' + tabName + '")');
        if (tab.length > 0) {
            tab.click();
        }
    }
});
