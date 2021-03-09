
// Add zoom to images.
$(document).ready(_ => mediumZoom(".main .content img"))
// Make external links open new window/tab.
$(document).ready(_ => $("a.reference.external").attr("target", "_blank"))

// TODO: take stuff from docsify to have tab params
// // Taken from: https://stackoverflow.com/a/8747204
// jQuery.expr[':'].icontains = (a, i, m) => jQuery(a).text().toUpperCase().indexOf(m[3].toUpperCase()) >= 0

// $(document).ready(_ => {
//     const tabName = new URLSearchParams(window.location.search).get('tab')
//     if (tabName == null) return
//     $(`.tab-label:icontains("${tabName}")`).click()
//     $("a.reference.internal").attr("href", (i, v) => v.replace())
// })
