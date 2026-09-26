using System;
using System.Collections.Generic;

namespace SoruKopyalama.Models
{
    public class PanelConfig
    {
        public string Name { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ActiveCookie { get; set; } = "";
        public DateTime LastLoginTime { get; set; }
    }

    public class AppSettings
    {
        public List<PanelConfig> Panels { get; set; } = new List<PanelConfig>
        {
            new PanelConfig { Name = "Final", Domain = "final.frns.in" },
            new PanelConfig { Name = "Limit", Domain = "limit.frns.in" },
            new PanelConfig { Name = "Esen", Domain = "esen.frns.in" }
        };

        public string DefaultSourcePanel { get; set; } = "Final";
        public string DefaultTargetPanel { get; set; } = "Final";
    }

    public class SistemSoruReferans
    {
        public string KaynakAdi { get; set; } = "";
        public string SoruNo { get; set; } = "";
        public string SolutionId { get; set; } = "";
        public string SourceId { get; set; } = "";
        public string CevapAnahtari { get; set; } = "";
        public string PanelDomain { get; set; } = "final.frns.in";
    }

    public class SoruIslemRaporu
    {
        public int SiraNo { get; set; }
        public string KisaKod { get; set; } = "";
        public string KodAcilimi { get; set; } = "";
        public string HedefSoruNo { get; set; } = "";
        public string HedefCevap { get; set; } = "";
        public string BulunanSolutionId { get; set; } = "";
        public string BulunanSourceId { get; set; } = "";
        public bool Basarili { get; set; }
        public string DurumMesaji { get; set; } = "";
        public DateTime IslemZamani { get; set; } = DateTime.Now;
    }
}
