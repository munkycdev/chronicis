namespace Chronicis.Client.Services;

/// <summary>
/// Provides Font Awesome icon data for the icon picker.
/// Icons are organized by category for easy browsing.
/// All icons are from Font Awesome Free 6.5 - verified to exist.
/// </summary>
public static class FontAwesomeIcons
{
    public static readonly List<IconCategory> Categories = new()
    {
        new IconCategory("Fantasy & Gaming", "fa-solid fa-dragon", new[]
        {
            "fa-solid fa-dragon", "fa-solid fa-dungeon", "fa-solid fa-dice-d20", "fa-solid fa-dice",
            "fa-solid fa-dice-d6", "fa-solid fa-chess", "fa-solid fa-chess-rook", "fa-solid fa-chess-knight",
            "fa-solid fa-chess-bishop", "fa-solid fa-chess-queen", "fa-solid fa-chess-king", "fa-solid fa-chess-pawn",
            "fa-solid fa-crown", "fa-solid fa-hat-wizard", "fa-solid fa-wand-magic-sparkles", "fa-solid fa-wand-sparkles",
            "fa-solid fa-scroll", "fa-solid fa-book-skull", "fa-solid fa-skull", "fa-solid fa-skull-crossbones",
            "fa-solid fa-ghost", "fa-solid fa-spider", "fa-solid fa-shield-halved", "fa-solid fa-shield",
            "fa-solid fa-gavel", "fa-solid fa-hammer", "fa-solid fa-khanda",
            "fa-solid fa-gem", "fa-solid fa-ring", "fa-solid fa-coins", "fa-solid fa-sack-dollar",
            "fa-solid fa-key", "fa-solid fa-lock", "fa-solid fa-unlock",
            "fa-solid fa-fire", "fa-solid fa-fire-flame-curved", "fa-solid fa-bolt",
            "fa-solid fa-bolt-lightning", "fa-solid fa-snowflake", "fa-solid fa-meteor",
            "fa-solid fa-explosion", "fa-solid fa-burst", "fa-solid fa-hand-sparkles", "fa-solid fa-hand-fist",
            "fa-solid fa-mask", "fa-solid fa-masks-theater", "fa-solid fa-eye",
            "fa-solid fa-eye-slash", "fa-solid fa-ankh", "fa-solid fa-cross", "fa-solid fa-star-of-david",
            "fa-solid fa-om", "fa-solid fa-yin-yang", "fa-solid fa-peace", "fa-solid fa-bahai",
            "fa-solid fa-book-open", "fa-solid fa-feather", "fa-solid fa-feather-pointed"
        }),

        new IconCategory("People & Characters", "fa-solid fa-user", new[]
        {
            "fa-solid fa-user", "fa-solid fa-user-tie", "fa-solid fa-user-ninja", "fa-solid fa-user-secret",
            "fa-solid fa-user-shield", "fa-solid fa-user-gear", "fa-solid fa-user-astronaut", "fa-solid fa-user-graduate",
            "fa-solid fa-user-nurse", "fa-solid fa-user-doctor", "fa-solid fa-user-injured", "fa-solid fa-user-plus",
            "fa-solid fa-user-minus", "fa-solid fa-user-pen", "fa-solid fa-user-lock", "fa-solid fa-user-check",
            "fa-solid fa-user-xmark", "fa-solid fa-user-clock", "fa-solid fa-user-tag", "fa-solid fa-user-group",
            "fa-solid fa-users", "fa-solid fa-users-gear", "fa-solid fa-users-line", "fa-solid fa-people-group",
            "fa-solid fa-people-arrows", "fa-solid fa-people-pulling", "fa-solid fa-person", "fa-solid fa-person-dress",
            "fa-solid fa-person-running", "fa-solid fa-person-walking", "fa-solid fa-person-hiking",
            "fa-solid fa-person-biking", "fa-solid fa-person-swimming", "fa-solid fa-person-skiing",
            "fa-solid fa-person-snowboarding", "fa-solid fa-person-falling", "fa-solid fa-person-drowning",
            "fa-solid fa-person-praying", "fa-solid fa-person-rays", "fa-solid fa-person-burst",
            "fa-solid fa-person-rifle", "fa-solid fa-person-military-rifle", "fa-solid fa-person-military-pointing",
            "fa-solid fa-child", "fa-solid fa-baby", "fa-solid fa-face-smile", "fa-solid fa-face-meh",
            "fa-solid fa-face-frown", "fa-solid fa-face-angry", "fa-solid fa-face-surprise", "fa-solid fa-face-laugh",
            "fa-solid fa-face-grin-stars", "fa-solid fa-face-dizzy", "fa-solid fa-head-side-virus"
        }),

        new IconCategory("Places & Buildings", "fa-solid fa-landmark", new[]
        {
            "fa-solid fa-house", "fa-solid fa-house-chimney", "fa-solid fa-building", "fa-solid fa-building-columns",
            "fa-solid fa-landmark", "fa-solid fa-landmark-dome", "fa-solid fa-landmark-flag",
            "fa-solid fa-church", "fa-solid fa-mosque", "fa-solid fa-synagogue", "fa-solid fa-place-of-worship",
            "fa-solid fa-torii-gate", "fa-solid fa-kaaba", "fa-solid fa-vihara", "fa-solid fa-gopuram",
            "fa-solid fa-hospital", "fa-solid fa-school", "fa-solid fa-hotel",
            "fa-solid fa-store", "fa-solid fa-warehouse", "fa-solid fa-industry", "fa-solid fa-city",
            "fa-solid fa-tent", "fa-solid fa-campground", "fa-solid fa-caravan", "fa-solid fa-igloo",
            "fa-solid fa-archway", "fa-solid fa-monument", "fa-solid fa-tower-observation", "fa-solid fa-oil-well",
            "fa-solid fa-bridge", "fa-solid fa-bridge-water", "fa-solid fa-road",
            "fa-solid fa-mountain", "fa-solid fa-mountain-sun", "fa-solid fa-mountain-city", "fa-solid fa-volcano",
            "fa-solid fa-tree", "fa-solid fa-tree-city", "fa-solid fa-seedling",
            "fa-solid fa-water", "fa-solid fa-anchor", "fa-solid fa-ship", "fa-solid fa-sailboat",
            "fa-solid fa-ferry", "fa-solid fa-compass", "fa-solid fa-map", "fa-solid fa-map-location",
            "fa-solid fa-map-location-dot", "fa-solid fa-location-dot", "fa-solid fa-location-pin",
            "fa-solid fa-globe", "fa-solid fa-earth-americas", "fa-solid fa-earth-europe", "fa-solid fa-earth-asia"
        }),

        new IconCategory("Nature & Animals", "fa-solid fa-paw", new[]
        {
            "fa-solid fa-paw", "fa-solid fa-dog", "fa-solid fa-cat", "fa-solid fa-horse", "fa-solid fa-horse-head",
            "fa-solid fa-cow", "fa-solid fa-hippo", "fa-solid fa-otter", "fa-solid fa-fish", "fa-solid fa-fish-fins",
            "fa-solid fa-shrimp", "fa-solid fa-frog", "fa-solid fa-crow", "fa-solid fa-dove", "fa-solid fa-kiwi-bird",
            "fa-solid fa-feather", "fa-solid fa-feather-pointed", "fa-solid fa-spider", "fa-solid fa-bug",
            "fa-solid fa-bugs", "fa-solid fa-locust", "fa-solid fa-mosquito", "fa-solid fa-worm",
            "fa-solid fa-tree", "fa-solid fa-leaf", "fa-solid fa-clover", "fa-solid fa-seedling",
            "fa-solid fa-plant-wilt", "fa-solid fa-cannabis", "fa-solid fa-wheat-awn", "fa-solid fa-apple-whole",
            "fa-solid fa-lemon", "fa-solid fa-carrot", "fa-solid fa-pepper-hot", "fa-solid fa-sun",
            "fa-solid fa-moon", "fa-solid fa-star", "fa-solid fa-cloud", "fa-solid fa-cloud-sun",
            "fa-solid fa-cloud-moon", "fa-solid fa-cloud-rain", "fa-solid fa-cloud-showers-heavy",
            "fa-solid fa-cloud-bolt", "fa-solid fa-snowflake", "fa-solid fa-wind", "fa-solid fa-tornado",
            "fa-solid fa-hurricane", "fa-solid fa-rainbow", "fa-solid fa-umbrella", "fa-solid fa-temperature-high",
            "fa-solid fa-temperature-low", "fa-solid fa-fire", "fa-solid fa-water", "fa-solid fa-droplet",
            "fa-solid fa-mountain", "fa-solid fa-volcano"
        }),

        new IconCategory("Objects & Items", "fa-solid fa-box", new[]
        {
            "fa-solid fa-box", "fa-solid fa-box-open", "fa-solid fa-boxes-stacked", "fa-solid fa-cube",
            "fa-solid fa-cubes", "fa-solid fa-bag-shopping", "fa-solid fa-basket-shopping", "fa-solid fa-cart-shopping",
            "fa-solid fa-gift", "fa-solid fa-gifts", "fa-solid fa-gem", "fa-solid fa-ring",
            "fa-solid fa-key", "fa-solid fa-lock", "fa-solid fa-unlock", "fa-solid fa-door-open",
            "fa-solid fa-door-closed", "fa-solid fa-chair", "fa-solid fa-couch", "fa-solid fa-bed",
            "fa-solid fa-bath", "fa-solid fa-sink", "fa-solid fa-utensils",
            "fa-solid fa-plate-wheat", "fa-solid fa-bowl-food", "fa-solid fa-mug-hot", "fa-solid fa-mug-saucer",
            "fa-solid fa-wine-glass", "fa-solid fa-wine-bottle", "fa-solid fa-beer-mug-empty", "fa-solid fa-martini-glass",
            "fa-solid fa-whiskey-glass", "fa-solid fa-bottle-water", "fa-solid fa-flask", "fa-solid fa-vial",
            "fa-solid fa-mortar-pestle", "fa-solid fa-prescription-bottle", "fa-solid fa-pills", "fa-solid fa-syringe",
            "fa-solid fa-bandage", "fa-solid fa-toolbox", "fa-solid fa-wrench", "fa-solid fa-screwdriver",
            "fa-solid fa-hammer", "fa-solid fa-gavel", "fa-solid fa-scissors", "fa-solid fa-pen",
            "fa-solid fa-pen-fancy", "fa-solid fa-pen-nib", "fa-solid fa-pencil", "fa-solid fa-brush",
            "fa-solid fa-paintbrush", "fa-solid fa-palette", "fa-solid fa-ruler", "fa-solid fa-compass-drafting",
            "fa-solid fa-magnifying-glass", "fa-solid fa-binoculars", "fa-solid fa-glasses", "fa-solid fa-hourglass",
            "fa-solid fa-hourglass-half", "fa-solid fa-hourglass-end", "fa-solid fa-clock", "fa-solid fa-stopwatch",
            "fa-solid fa-bell", "fa-solid fa-lightbulb",
            "fa-solid fa-camera", "fa-solid fa-scroll",
            "fa-solid fa-book", "fa-solid fa-book-open", "fa-solid fa-bookmark", "fa-solid fa-newspaper"
        }),

        new IconCategory("Weapons & Combat", "fa-solid fa-shield-halved", new[]
        {
            "fa-solid fa-shield", "fa-solid fa-shield-halved", "fa-solid fa-shield-heart", "fa-solid fa-shield-virus",
            "fa-solid fa-gavel", "fa-solid fa-hammer", "fa-solid fa-khanda",
            "fa-solid fa-gun", "fa-solid fa-crosshairs", "fa-solid fa-bullseye",
            "fa-solid fa-bomb", "fa-solid fa-explosion", "fa-solid fa-burst", "fa-solid fa-hand-fist",
            "fa-solid fa-skull", "fa-solid fa-skull-crossbones", "fa-solid fa-bone", "fa-solid fa-cross",
            "fa-solid fa-fire", "fa-solid fa-fire-flame-curved", "fa-solid fa-bolt", "fa-solid fa-bolt-lightning",
            "fa-solid fa-meteor", "fa-solid fa-radiation", "fa-solid fa-biohazard", "fa-solid fa-triangle-exclamation",
            "fa-solid fa-helmet-safety", "fa-solid fa-vest", "fa-solid fa-vest-patches", "fa-solid fa-jet-fighter",
            "fa-solid fa-helicopter", "fa-solid fa-person-rifle", "fa-solid fa-person-military-rifle"
        }),

        new IconCategory("Transport & Travel", "fa-solid fa-car", new[]
        {
            "fa-solid fa-car", "fa-solid fa-car-side", "fa-solid fa-truck", "fa-solid fa-truck-pickup",
            "fa-solid fa-bus", "fa-solid fa-train", "fa-solid fa-train-subway", "fa-solid fa-train-tram",
            "fa-solid fa-taxi", "fa-solid fa-bicycle", "fa-solid fa-motorcycle", "fa-solid fa-horse",
            "fa-solid fa-ship", "fa-solid fa-sailboat", "fa-solid fa-ferry", "fa-solid fa-anchor",
            "fa-solid fa-plane", "fa-solid fa-plane-departure", "fa-solid fa-plane-arrival", "fa-solid fa-helicopter",
            "fa-solid fa-rocket", "fa-solid fa-shuttle-space", "fa-solid fa-satellite", "fa-solid fa-road",
            "fa-solid fa-route", "fa-solid fa-map", "fa-solid fa-compass", "fa-solid fa-location-dot",
            "fa-solid fa-suitcase", "fa-solid fa-suitcase-rolling", "fa-solid fa-passport", "fa-solid fa-ticket",
            "fa-solid fa-gas-pump", "fa-solid fa-charging-station", "fa-solid fa-trailer", "fa-solid fa-caravan"
        }),

        new IconCategory("Communication", "fa-solid fa-comment", new[]
        {
            "fa-solid fa-comment", "fa-solid fa-comment-dots", "fa-solid fa-comments", "fa-solid fa-message",
            "fa-solid fa-envelope", "fa-solid fa-envelope-open", "fa-solid fa-paper-plane", "fa-solid fa-inbox",
            "fa-solid fa-phone", "fa-solid fa-phone-volume", "fa-solid fa-mobile", "fa-solid fa-mobile-screen",
            "fa-solid fa-tablet", "fa-solid fa-laptop", "fa-solid fa-desktop", "fa-solid fa-tv",
            "fa-solid fa-radio", "fa-solid fa-podcast", "fa-solid fa-microphone", "fa-solid fa-microphone-lines",
            "fa-solid fa-headphones", "fa-solid fa-volume-high", "fa-solid fa-volume-low", "fa-solid fa-volume-off",
            "fa-solid fa-bell", "fa-solid fa-bullhorn", "fa-solid fa-tower-broadcast", "fa-solid fa-satellite-dish",
            "fa-solid fa-wifi", "fa-solid fa-signal", "fa-solid fa-rss", "fa-solid fa-hashtag",
            "fa-solid fa-at", "fa-solid fa-link", "fa-solid fa-share", "fa-solid fa-share-nodes",
            "fa-solid fa-retweet", "fa-solid fa-quote-left", "fa-solid fa-quote-right", "fa-solid fa-language"
        }),

        new IconCategory("Files & Documents", "fa-solid fa-file", new[]
        {
            "fa-solid fa-file", "fa-solid fa-file-lines", "fa-solid fa-file-pdf", "fa-solid fa-file-word",
            "fa-solid fa-file-excel", "fa-solid fa-file-powerpoint", "fa-solid fa-file-image", "fa-solid fa-file-video",
            "fa-solid fa-file-audio", "fa-solid fa-file-code", "fa-solid fa-file-zipper", "fa-solid fa-file-csv",
            "fa-solid fa-file-contract", "fa-solid fa-file-signature", "fa-solid fa-file-invoice", "fa-solid fa-file-medical",
            "fa-solid fa-file-prescription", "fa-solid fa-file-waveform", "fa-solid fa-file-arrow-up", "fa-solid fa-file-arrow-down",
            "fa-solid fa-file-export", "fa-solid fa-file-import", "fa-solid fa-file-pen", "fa-solid fa-file-circle-plus",
            "fa-solid fa-file-circle-minus", "fa-solid fa-file-circle-check", "fa-solid fa-file-circle-xmark",
            "fa-solid fa-folder", "fa-solid fa-folder-open", "fa-solid fa-folder-plus", "fa-solid fa-folder-minus",
            "fa-solid fa-folder-tree", "fa-solid fa-copy", "fa-solid fa-paste", "fa-solid fa-clipboard",
            "fa-solid fa-clipboard-list", "fa-solid fa-clipboard-check", "fa-solid fa-note-sticky", "fa-solid fa-book",
            "fa-solid fa-book-open", "fa-solid fa-book-bookmark", "fa-solid fa-book-journal-whills", "fa-solid fa-book-atlas",
            "fa-solid fa-newspaper", "fa-solid fa-scroll", "fa-solid fa-receipt", "fa-solid fa-certificate"
        }),

        new IconCategory("Science & Medical", "fa-solid fa-flask", new[]
        {
            "fa-solid fa-flask", "fa-solid fa-flask-vial", "fa-solid fa-vial", "fa-solid fa-vials",
            "fa-solid fa-microscope", "fa-solid fa-atom", "fa-solid fa-dna", "fa-solid fa-virus",
            "fa-solid fa-bacteria", "fa-solid fa-disease", "fa-solid fa-biohazard", "fa-solid fa-radiation",
            "fa-solid fa-brain", "fa-solid fa-heart", "fa-solid fa-heart-pulse", "fa-solid fa-lungs",
            "fa-solid fa-lungs-virus", "fa-solid fa-bone", "fa-solid fa-tooth", "fa-solid fa-eye",
            "fa-solid fa-ear-listen", "fa-solid fa-hand", "fa-solid fa-hospital", "fa-solid fa-stethoscope",
            "fa-solid fa-syringe", "fa-solid fa-pills", "fa-solid fa-tablets", "fa-solid fa-capsules",
            "fa-solid fa-prescription", "fa-solid fa-prescription-bottle", "fa-solid fa-prescription-bottle-medical",
            "fa-solid fa-bandage", "fa-solid fa-kit-medical", "fa-solid fa-thermometer", "fa-solid fa-x-ray",
            "fa-solid fa-user-doctor", "fa-solid fa-user-nurse", "fa-solid fa-bed-pulse", "fa-solid fa-wheelchair",
            "fa-solid fa-crutch", "fa-solid fa-weight-scale", "fa-solid fa-pump-medical", "fa-solid fa-mortar-pestle"
        }),

        new IconCategory("Music & Entertainment", "fa-solid fa-music", new[]
        {
            "fa-solid fa-music", "fa-solid fa-guitar", "fa-solid fa-drum", "fa-solid fa-drum-steelpan",
            "fa-solid fa-headphones", "fa-solid fa-headphones-simple", "fa-solid fa-microphone", "fa-solid fa-microphone-lines",
            "fa-solid fa-radio", "fa-solid fa-podcast", "fa-solid fa-record-vinyl", "fa-solid fa-compact-disc",
            "fa-solid fa-film", "fa-solid fa-video", "fa-solid fa-camera", "fa-solid fa-camera-retro",
            "fa-solid fa-clapperboard", "fa-solid fa-photo-film", "fa-solid fa-tv", "fa-solid fa-gamepad",
            "fa-solid fa-dice", "fa-solid fa-dice-d20", "fa-solid fa-puzzle-piece", "fa-solid fa-chess",
            "fa-solid fa-ticket", "fa-solid fa-masks-theater", "fa-solid fa-palette",
            "fa-solid fa-paintbrush", "fa-solid fa-brush", "fa-solid fa-spray-can", "fa-solid fa-image",
            "fa-solid fa-icons", "fa-solid fa-face-grin-stars", "fa-solid fa-wand-magic-sparkles", "fa-solid fa-hat-wizard"
        }),

        new IconCategory("Symbols & Shapes", "fa-solid fa-shapes", new[]
        {
            "fa-solid fa-circle", "fa-solid fa-square", "fa-solid fa-triangle-exclamation", "fa-solid fa-diamond",
            "fa-solid fa-star", "fa-solid fa-star-half", "fa-solid fa-heart", "fa-solid fa-heart-crack",
            "fa-solid fa-bookmark", "fa-solid fa-flag", "fa-solid fa-flag-checkered", "fa-solid fa-certificate",
            "fa-solid fa-award", "fa-solid fa-medal", "fa-solid fa-trophy", "fa-solid fa-crown",
            "fa-solid fa-check", "fa-solid fa-xmark", "fa-solid fa-plus", "fa-solid fa-minus",
            "fa-solid fa-equals", "fa-solid fa-divide", "fa-solid fa-percent", "fa-solid fa-infinity",
            "fa-solid fa-hashtag", "fa-solid fa-at", "fa-solid fa-question",
            "fa-solid fa-exclamation", "fa-solid fa-quote-left", "fa-solid fa-quote-right", "fa-solid fa-copyright",
            "fa-solid fa-registered", "fa-solid fa-trademark", "fa-solid fa-circle-info", "fa-solid fa-circle-question",
            "fa-solid fa-circle-exclamation", "fa-solid fa-circle-check", "fa-solid fa-circle-xmark",
            "fa-solid fa-circle-plus", "fa-solid fa-circle-minus", "fa-solid fa-ban", "fa-solid fa-slash",
            "fa-solid fa-arrows-rotate", "fa-solid fa-rotate", "fa-solid fa-rotate-left", "fa-solid fa-rotate-right"
        }),

        new IconCategory("Arrows & Navigation", "fa-solid fa-arrow-right", new[]
        {
            "fa-solid fa-arrow-up", "fa-solid fa-arrow-down", "fa-solid fa-arrow-left", "fa-solid fa-arrow-right",
            "fa-solid fa-arrow-up-long", "fa-solid fa-arrow-down-long", "fa-solid fa-arrow-left-long", "fa-solid fa-arrow-right-long",
            "fa-solid fa-arrows-up-down", "fa-solid fa-arrows-left-right", "fa-solid fa-arrows-up-down-left-right",
            "fa-solid fa-up-down-left-right", "fa-solid fa-arrow-up-right-from-square", "fa-solid fa-arrow-right-from-bracket",
            "fa-solid fa-arrow-right-to-bracket", "fa-solid fa-arrow-turn-down", "fa-solid fa-arrow-turn-up",
            "fa-solid fa-chevron-up", "fa-solid fa-chevron-down", "fa-solid fa-chevron-left", "fa-solid fa-chevron-right",
            "fa-solid fa-angles-up", "fa-solid fa-angles-down", "fa-solid fa-angles-left", "fa-solid fa-angles-right",
            "fa-solid fa-caret-up", "fa-solid fa-caret-down", "fa-solid fa-caret-left", "fa-solid fa-caret-right",
            "fa-solid fa-circle-arrow-up", "fa-solid fa-circle-arrow-down", "fa-solid fa-circle-arrow-left", "fa-solid fa-circle-arrow-right",
            "fa-solid fa-square-arrow-up-right", "fa-solid fa-share", "fa-solid fa-reply", "fa-solid fa-reply-all",
            "fa-solid fa-shuffle", "fa-solid fa-repeat", "fa-solid fa-retweet", "fa-solid fa-recycle"
        }),

        new IconCategory("Interface & UI", "fa-solid fa-gear", new[]
        {
            "fa-solid fa-gear", "fa-solid fa-gears", "fa-solid fa-sliders", "fa-solid fa-bars",
            "fa-solid fa-ellipsis", "fa-solid fa-ellipsis-vertical", "fa-solid fa-grip", "fa-solid fa-grip-vertical",
            "fa-solid fa-grip-lines", "fa-solid fa-grip-lines-vertical", "fa-solid fa-house", "fa-solid fa-magnifying-glass",
            "fa-solid fa-magnifying-glass-plus", "fa-solid fa-magnifying-glass-minus", "fa-solid fa-filter",
            "fa-solid fa-sort", "fa-solid fa-sort-up", "fa-solid fa-sort-down", "fa-solid fa-list",
            "fa-solid fa-list-ul", "fa-solid fa-list-ol", "fa-solid fa-list-check", "fa-solid fa-table",
            "fa-solid fa-table-cells", "fa-solid fa-table-columns", "fa-solid fa-table-list", "fa-solid fa-border-all",
            "fa-solid fa-maximize", "fa-solid fa-minimize", "fa-solid fa-expand", "fa-solid fa-compress",
            "fa-solid fa-up-right-and-down-left-from-center", "fa-solid fa-down-left-and-up-right-to-center",
            "fa-solid fa-download", "fa-solid fa-upload", "fa-solid fa-cloud-arrow-up", "fa-solid fa-cloud-arrow-down",
            "fa-solid fa-trash", "fa-solid fa-trash-can", "fa-solid fa-pen", "fa-solid fa-pen-to-square",
            "fa-solid fa-copy", "fa-solid fa-paste", "fa-solid fa-scissors", "fa-solid fa-floppy-disk",
            "fa-solid fa-eye", "fa-solid fa-eye-slash", "fa-solid fa-lock", "fa-solid fa-unlock",
            "fa-solid fa-user", "fa-solid fa-circle-user", "fa-solid fa-right-from-bracket", "fa-solid fa-right-to-bracket"
        })
    };

    /// <summary>
    /// Get all icons flattened into a single list (for searching)
    /// </summary>
    public static List<string> GetAllIcons()
    {
        return Categories
            .SelectMany(c => c.Icons)
            .Distinct()
            .OrderBy(i => i)
            .ToList();
    }

    /// <summary>
    /// Search icons by name
    /// </summary>
    public static List<string> SearchIcons(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAllIcons();

        var searchTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return GetAllIcons()
            .Where(icon =>
            {
                var iconName = icon.Replace("fa-solid fa-", "").Replace("-", " ");
                return searchTerms.All(term => iconName.Contains(term));
            })
            .ToList();
    }
}

public class IconCategory
{
    public string Name { get; }
    public string Icon { get; }
    public string[] Icons { get; }

    public IconCategory(string name, string icon, string[] icons)
    {
        Name = name;
        Icon = icon;
        Icons = icons;
    }
}
