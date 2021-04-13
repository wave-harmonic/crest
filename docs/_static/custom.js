
// Add zoom to images.
$(document).ready(_ => mediumZoom(".main .content img"))
// Make external links open new window/tab.
$(document).ready(_ => $("a.reference.external").attr("target", "_blank"))

// Taken from: https://stackoverflow.com/a/8747204
jQuery.expr[':'].icontains = (a, i, m) => jQuery(a).text().toUpperCase().indexOf(m[3].toUpperCase()) >= 0

// Adapted from: https://stackoverflow.com/a/4106957
jQuery.fn.textNodes = function () {
    return this.contents().filter(function () {
        return (this.nodeType === Node.TEXT_NODE && this.nodeValue.trim() !== "")
    })
}

const RENDER_PIPELINES = [
    "birp",
    "hdrp",
    "urp",
]

const isLocalHost = location.hostname === "localhost" || location.hostname === "127.0.0.1" || location.hostname === ""
const isLatest = window.location.pathname.startsWith("/en/latest/") || isLocalHost
const isStable = window.location.pathname.startsWith("/en/stable/")
const version = isLocalHost ? "latest" : window.location.pathname.split("/").filter(x => x)[1]
// NOTE: regex could be expanded to support pre-release versions (eg 1.0-alpha).
const isVersion = !isLatest && !isStable && /\d+(?:\.\d+)*/.test(version)

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

    // Add "(unreleased)" to the latest version when not viewing a stable release.
    if (isLatest && window.location.pathname.endsWith("history.html")) {
        const headingNode = $("#version h2").textNodes().first()
        headingNode.replaceWith(headingNode.text() + " (unreleased)")
    }

    if (isLatest) {
        // Adapted from:
        // https://github.com/godotengine/godot-docs/blob/21979b61badb5dd9d46c9859824fd0f0c0205bbd/_static/js/custom.js#L212-L228
        // Add a compatibility notice using JavaScript so it doesn't end up in the automatically generated
        // `meta description` tag.
        const stableUrl = location.href.replace('/latest/', '/stable/')
        $("article[role='main']").prepend(`
            <div class="admonition attention">
                <p class="admonition-title">Attention</p>
                <p>
                    You are reading the <code class="docutils literal notranslate"><span class="pre">latest</span></code>
                    (unstable) version of this documentation, which may document features not available
                    or compatible with the latest <em>Crest</em> packages released on the <em>Unity Asset Store</em>.
                </p>
                <p class="last">
                    View the <a class="reference" href="${stableUrl}">stable version of this page</a>.
                </p>
            </div>
        `)
    }
})

if (isPage404 && !isVersion) {
    $.ajax({
        type: "HEAD",
        url: `/en/${version}/`,
        error: _ => {
            var newUrl = window.location.href.replace(`/${version}/`, "/latest/")
            $("#404-page-script").before(`
                <div id="404-admonition" class="admonition attention">
                    <p class="admonition-title">Attention</p>
                    <p class="last">
                        Looks like you are on a version without a published tag.
                        We will redirect you to the latest documentation automatically.
                        If it does not redirect automatically in a few seconds, please click the follow:
                        <a href="${newUrl}">${newUrl}</a>
                    </p>
                </div>
            `)

            // TODO: enable once tested on live server.
            // window.location.replace(newUrl)
        }
    })
}
