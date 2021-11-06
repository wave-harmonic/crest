// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Internal
{
    public static class Constants
    {
        const string PREFIX = "Crest ";
        public const string MENU_SCRIPTS = "Scripts/Crest/";
        public const string MENU_PREFIX_SCRIPTS = MENU_SCRIPTS + PREFIX;
        public const string MENU_PREFIX_INTERNAL = MENU_SCRIPTS + "Internal/";
        public const string MENU_PREFIX_DEBUG = MENU_SCRIPTS + "Debug/" + PREFIX;
        public const string MENU_PREFIX_SPLINE = MENU_SCRIPTS + "Spline/" + PREFIX;
        public const string MENU_PREFIX_EXAMPLE = MENU_SCRIPTS + "Example/" + PREFIX;

        // Usage: HelpURL(HELP_URL_BASE_USER + "page.html" + HELP_URL_RP + "#heading")
        // HELP_URL_VERSION should be updated AFTER a new tag is made. The 404 page for the documentation will redirect
        // the user to "latest" since the tag for the new version is not published yet.
        // For example, if 4.9 was just released, so we change HELP_URL_VERSION from 4.9 to 4.10. If a user is using
        // master, then they will be redirected from crest.readthedocs.io/en/4.10 to crest.readthedocs.io/en/latest when
        // they land on the 404 page.
        public const string HELP_URL_VERSION = "4.15";
        public const string HELP_URL_RP = "?rp=birp";
        public const string HELP_URL_BASE = "https://crest.readthedocs.io/en/" + HELP_URL_VERSION + "/";
        public const string HELP_URL_BASE_USER = HELP_URL_BASE + "user/";
        public const string HELP_URL_GENERAL = HELP_URL_BASE + HELP_URL_RP;
    }
}
