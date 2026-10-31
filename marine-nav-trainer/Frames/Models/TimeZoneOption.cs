namespace marine_nav_trainer.Navigation.Models {
    public class TimeZoneOption {
        public string Name { get; init; } = string.Empty;
        public string Offset { get; init; } = "+00:00";

        public string Display => $"{Name} ({Offset})";

        // Strefy od -12:00 do +14:00
        // Segment napisany z pomocą SI: Claude
        public static IReadOnlyList<TimeZoneOption> All { get; } = [
            Zone("AoE", "-12:00"),
            Zone("SST", "-11:00"),
            Zone("NUT", "-11:00"),
            Zone("HST", "-10:00"),
            Zone("HDT", "-09:00"),
            Zone("MART", "-09:30"),
            Zone("AKST", "-09:00"),
            Zone("AKDT", "-08:00"),
            Zone("PST", "-08:00"),
            Zone("PDT", "-07:00"),
            Zone("MST", "-07:00"),
            Zone("MDT", "-06:00"),
            Zone("CST", "-06:00"),
            Zone("CDT", "-05:00"),
            Zone("EST", "-05:00"),
            Zone("COT", "-05:00"),
            Zone("EDT", "-04:00"),
            Zone("AST", "-04:00"),
            Zone("CLT", "-04:00"),
            Zone("NST", "-03:30"),
            Zone("ADT", "-03:00"),
            Zone("ART", "-03:00"),
            Zone("BRT", "-03:00"),
            Zone("CLST", "-03:00"),
            Zone("NDT", "-02:30"),
            Zone("FNT", "-02:00"),
            Zone("WGST", "-02:00"),
            Zone("CVT", "-01:00"),
            Zone("AZOT", "-01:00"),
            Zone("UTC", "+00:00"),
            Zone("GMT", "+00:00"),
            Zone("WET", "+00:00"),
            Zone("AZOST", "+00:00"),
            Zone("CET", "+01:00"),
            Zone("WAT", "+01:00"),
            Zone("WEST", "+01:00"),
            Zone("BST", "+01:00"),
            Zone("IST", "+01:00"),
            Zone("EET", "+02:00"),
            Zone("CAT", "+02:00"),
            Zone("SAST", "+02:00"),
            Zone("CEST", "+02:00"),
            Zone("MSK", "+03:00"),
            Zone("EAT", "+03:00"),
            Zone("AST", "+03:00"),
            Zone("EEST", "+03:00"),
            Zone("IDT", "+03:00"),
            Zone("IRST", "+03:30"),
            Zone("GST", "+04:00"),
            Zone("AFT", "+04:30"),
            Zone("PKT", "+05:00"),
            Zone("UZT", "+05:00"),
            Zone("IST", "+05:30"),
            Zone("NPT", "+05:45"),
            Zone("BST", "+06:00"),
            Zone("MMT", "+06:30"),
            Zone("ICT", "+07:00"),
            Zone("WIB", "+07:00"),
            Zone("CST", "+08:00"),
            Zone("AWST", "+08:00"),
            Zone("ACWST", "+08:45"),
            Zone("JST", "+09:00"),
            Zone("KST", "+09:00"),
            Zone("ACST", "+09:30"),
            Zone("AEST", "+10:00"),
            Zone("ACDT", "+10:30"),
            Zone("LHST", "+10:30"),
            Zone("SBT", "+11:00"),
            Zone("AEDT", "+11:00"),
            Zone("LHDT", "+11:00"),
            Zone("NZST", "+12:00"),
            Zone("FJT", "+12:00"),
            Zone("CHAST", "+12:45"),
            Zone("TOT", "+13:00"),
            Zone("NZDT", "+13:00"),
            Zone("CHADT", "+13:45"),
            Zone("LINT", "+14:00")
        ];

        private static TimeZoneOption Zone(string name, string offset) =>
            new() { Name = name, Offset = offset };
        // Koniec segmentu SI
    }
}
