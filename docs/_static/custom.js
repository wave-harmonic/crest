
// Add zoom to images.
$(document).ready(_ => mediumZoom(".main .content img"))
// Make external links open new window/tab.
$(document).ready(_ => $("a.reference.external").attr("target", "_blank"))

// Taken from: https://stackoverflow.com/a/8747204
jQuery.expr[':'].icontains = (a, i, m) => jQuery(a).text().toUpperCase().indexOf(m[3].toUpperCase()) >= 0

const RENDER_PIPELINES = [
    "birp",
    "hdrp",
    "urp",
]

function updateLinksWithRenderPipeline(renderPipeline) {
    $("a.reference.internal").attr("href", (_, href) => {
        const url = new URL(href, window.location)
        url.searchParams.set("rp", renderPipeline)
        // We are replacing Sphinx's relative URLs with absolute URLs as a side-effect. Should be okay.
        return url.href
    })

}

function updateLocationWithRenderPipeline(renderPipeline) {
    const url = new URL(window.location)
    url.searchParams.set("rp", renderPipeline)
    // Changes the URL without reloading.
    window.history.pushState({}, "", url)
}

// Add support for RP URL parameter.
$(document).ready(_ => {
    const renderPipeline = new URLSearchParams(window.location.search).get('rp')
    if (renderPipeline != null) {
        $(`.tab-label:icontains("${renderPipeline}")`).click()
        updateLinksWithRenderPipeline(renderPipeline)
    }
    $(".tab-label").click(x => {
        const tabName = $(x.target).text().toLowerCase()
        // Check that the tab is actually a render pipeline tab.
        if (RENDER_PIPELINES.includes(tabName)) {
            updateLinksWithRenderPipeline(tabName)
            updateLocationWithRenderPipeline(tabName)
        }
    })
})
