/* globals $ */

var smapi = smapi || {};
var app;
smapi.modList = function (mods, enableBeta) {
    // init data
    var defaultStats = {
        total: 0,
        compatible: 0,
        workaround: 0,
        soon: 0,
        broken: 0,
        abandoned: 0,
        invalid: 0
    };
    var data = {
        mods: mods,
        showAdvanced: false,
        visibleStats: $.extend({}, defaultStats),
        filters: {
            source: {
                value: {
                    open: { value: true },
                    closed: { value: true }
                }
            },
            status: {
                label: enableBeta ? "main status" : "status",
                value: {
                    // note: keys must match status returned by the API
                    ok: { value: true },
                    optional: { value: true },
                    unofficial: { value: true },
                    workaround: { value: true },
                    broken: { value: true },
                    abandoned: { value: true },
                    obsolete: { value: true }
                }
            },
            betaStatus: {
                label: "beta status",
                value: {} // cloned from status field if needed
            },
            download: {
                value: {
                    chucklefish: { value: true, label: "Chucklefish" },
                    curseforge: { value: true, label: "CurseForge" },
                    moddrop: { value: true, label: "ModDrop" },
                    nexus: { value: true, label: "Nexus" },
                    custom: { value: true }
                }
            }
        },
        search: ""
    };

    // init filters
    Object.entries(data.filters).forEach(([groupKey, filterGroup]) => {
        filterGroup.label = filterGroup.label || groupKey;
        Object.entries(filterGroup.value).forEach(([filterKey, filter]) => {
            filter.id = ("filter_" + groupKey + "_" + filterKey).replace(/[^a-zA-Z0-9]/g, "_");
            filter.label = filter.label || filterKey;
        });
    });

    // init beta filters
    if (enableBeta) {
        var filterGroup = data.filters.betaStatus;
        $.extend(true, filterGroup.value, data.filters.status.value);
        Object.entries(filterGroup.value).forEach(([filterKey, filter]) => {
            filter.id = "beta_" + filter.id;
        });
    }
    else
        delete data.filters.betaStatus;

    // init mods
    for (var i = 0; i < data.mods.length; i++) {
        var mod = mods[i];

        // set initial visibility
        mod.Visible = true;

        // set overall compatibility
        mod.LatestCompatibility = mod.BetaCompatibility || mod.Compatibility;

        // concatenate searchable text
        mod.SearchableText = [mod.Name, mod.AlternateNames, mod.Author, mod.AlternateAuthors, mod.Compatibility.Summary, mod.BrokeIn];
        if (mod.Compatibility.UnofficialVersion)
            mod.SearchableText.push(mod.Compatibility.UnofficialVersion);
        if (mod.BetaCompatibility) {
            mod.SearchableText.push(mod.BetaCompatibility.Summary);
            if (mod.BetaCompatibility.UnofficialVersion)
                mod.SearchableText.push(mod.BetaCompatibility.UnofficialVersion);
        }
        for (var p = 0; p < mod.ModPages; p++)
            mod.SearchableField.push(mod.ModPages[p].Text);
        mod.SearchableText = mod.SearchableText.join(" ").toLowerCase();
    }

    // init app
    app = new Vue({
        el: "#app",
        data: data,
        mounted: function () {
            // enable table sorting
            $("#mod-list").tablesorter({
                cssHeader: "header",
                cssAsc: "headerSortUp",
                cssDesc: "headerSortDown"
            });

            // put focus in textbox for quick search
            if (!location.hash)
                $("#search-box").focus();

            // jump to anchor (since table is added after page load)
            this.fixHashPosition();
        },
        methods: {
            /**
             * Update the visibility of all mods based on the current search text and filters.
             */
            applyFilters: function () {
                // get search terms
                var words = data.search.toLowerCase().split(" ");

                // apply criteria
                var stats = data.visibleStats = $.extend({}, defaultStats);
                for (var i = 0; i < data.mods.length; i++) {
                    var mod = data.mods[i];
                    mod.Visible = true;

                    // check filters
                    mod.Visible = this.matchesFilters(mod, words);
                    if (mod.Visible) {
                        stats.total++;
                        stats[this.getCompatibilityGroup(mod)]++;
                    }
                }
            },

            /**
             * Fix the window position for the current hash.
             */
            fixHashPosition: function () {
                if (!location.hash)
                    return;

                var row = $(location.hash);
                var target = row.prev().get(0) || row.get(0);
                if (target)
                    target.scrollIntoView();
            },

            /**
             * Get whether a mod matches the current filters.
             * @param {object} mod The mod to check.
             * @param {string[]} searchWords The search words to match.
             * @returns {bool} Whether the mod matches the filters.
             */
            matchesFilters: function (mod, searchWords) {
                var filters = data.filters;

                // check hash
                if (location.hash === "#" + mod.Slug)
                    return true;

                // check source
                if (!filters.source.value.open.value && mod.SourceUrl)
                    return false;
                if (!filters.source.value.closed.value && !mod.SourceUrl)
                    return false;

                // check status
                var mainStatus = mod.Compatibility.Status;
                if (filters.status.value[mainStatus] && !filters.status.value[mainStatus].value)
                    return false;

                // check beta status
                if (enableBeta) {
                    var betaStatus = mod.LatestCompatibility.Status;
                    if (filters.betaStatus.value[betaStatus] && !filters.betaStatus.value[betaStatus].value)
                        return false;
                }

                // check download sites
                var ignoreSites = [];

                if (!filters.download.value.chucklefish.value)
                    ignoreSites.push("Chucklefish");
                if (!filters.download.value.curseforge.value)
                    ignoreSites.push("CurseForge");
                if (!filters.download.value.moddrop.value)
                    ignoreSites.push("ModDrop");
                if (!filters.download.value.nexus.value)
                    ignoreSites.push("Nexus");
                if (!filters.download.value.custom.value)
                    ignoreSites.push("custom");

                if (ignoreSites.length) {
                    var anyLeft = false;
                    for (var i = 0; i < mod.ModPageSites.length; i++) {
                        if (ignoreSites.indexOf(mod.ModPageSites[i]) === -1) {
                            anyLeft = true;
                            break;
                        }
                    }

                    if (!anyLeft)
                        return false;
                }

                // check search terms
                for (var w = 0; w < searchWords.length; w++) {
                    if (mod.SearchableText.indexOf(searchWords[w]) === -1)
                        return false;
                }

                return true;
            },

            /**
             * Get a mod's compatibility group for mod metrics.
             * @param {object} mod The mod to check.
             * @returns {string} The compatibility group (one of 'compatible', 'workaround', 'soon', 'broken', 'abandoned', or 'invalid').
             */
            getCompatibilityGroup: function (mod) {
                var status = mod.LatestCompatibility.Status;
                switch (status) {
                    // obsolete
                    case "abandoned":
                    case "obsolete":
                        return "abandoned";

                    // compatible
                    case "ok":
                    case "optional":
                        return "compatible";

                    // workaround
                    case "workaround":
                    case "unofficial":
                        return "workaround";

                    // soon/broken
                    case "broken":
                        if (mod.SourceUrl)
                            return "soon";
                        else
                            return "broken";

                    default:
                        return "invalid";
                }
            }
        }
    });
    app.applyFilters();
    app.fixHashPosition();
    window.addEventListener("hashchange", function () {
        app.applyFilters();
        app.fixHashPosition();
    });
};
