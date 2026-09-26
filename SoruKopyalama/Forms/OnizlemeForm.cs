using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SoruKopyalama.Models;
using SoruKopyalama.Services;

namespace SoruKopyalama.Forms
{
    /// <summary>
    /// Kopyalamadan önce her iş emri satırının hangi kaynak soruya eşleştiğini gösterir.
    /// Kullanıcı yanlış/eksik eşleşmeye doğru Soru ID'yi yazarsa bu kalıcı düzeltme olarak kaydedilir.
    /// </summary>
    public class OnizlemeForm : Form
    {
        private readonly List<IsEmriSatiri> _satirlar;
        private readonly string _panel;
        private readonly DatabaseManager _db;
        private readonly Eslestirici _eslestirici;

        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _lblOzet = new Label();
        private readonly Button _btnKopyala = new Button();
        private readonly Button _btnIptal = new Button();

        private bool _yukleniyor;

        private const string ColSec = "Sec", ColSira = "Sira", ColKod = "Kod", ColBrans = "Brans", ColHedef = "Hedef",
                             ColCevap = "Cevap", ColDurum = "Durum", ColSoruId = "SoruId", ColKaynakId = "KaynakId",
                             ColDbCevap = "DbCevap", ColZorluk = "Zorluk", ColKazanim = "Kazanim", ColYol = "Yol", ColAciklama = "Aciklama";

        public OnizlemeForm(List<IsEmriSatiri> satirlar, string panel, DatabaseManager db, Eslestirici eslestirici)
        {
            _satirlar = satirlar;
            _panel = panel;
            _db = db;
            _eslestirici = eslestirici;

            Text = $"🔍 Ön Kontrol - [{panel}] paneli";
            Size = new Size(1400, 780);
            MinimumSize = new Size(1000, 500);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(244, 246, 249);
            Icon = LogoHelper.CreateLimitIcon();

            ArayuzuKur();
            Doldur();
        }

        private void ArayuzuKur()
        {
            var ust = new Panel { Dock = DockStyle.Top, Height = 78, Padding = new Padding(10, 6, 10, 0) };
            var aciklama = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 9.5F),
                Text = "Yeşil satırlar kesin eşleşmedir ve seçili gelir. Sarı/kırmızı satırları kontrol edin.\n" +
                       "Yanlış veya bulunamayan bir soru için doğru Soru ID'yi 'Soru ID' hücresine yazın: program bunu kalıcı olarak hatırlar."
            };
            var secimBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, FlowDirection = FlowDirection.LeftToRight };
            secimBar.Controls.Add(SecimButonu("✅ Sadece kesinleri seç", () => TopluSec(s => Eslestirici.VarsayilanSecili(s))));
            secimBar.Controls.Add(SecimButonu("☑ Kaynağı olan tümünü seç", () => TopluSec(SecilebilirMi)));
            secimBar.Controls.Add(SecimButonu("☐ Seçimi kaldır", () => TopluSec(_ => false)));
            ust.Controls.Add(secimBar);
            ust.Controls.Add(aciklama);

            var alt = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10) };

            _btnKopyala.Text = "🚀 SEÇİLİ SORULARI KOPYALA";
            _btnKopyala.Dock = DockStyle.Right;
            _btnKopyala.Width = 300;
            _btnKopyala.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnKopyala.BackColor = Color.FromArgb(39, 174, 96);
            _btnKopyala.ForeColor = Color.White;
            _btnKopyala.FlatStyle = FlatStyle.Flat;
            _btnKopyala.FlatAppearance.BorderSize = 0;
            _btnKopyala.Click += (s, e) =>
            {
                _grid.EndEdit();
                if (!_satirlar.Any(x => x.Secili))
                {
                    MessageBox.Show("Kopyalanacak seçili satır yok.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            _btnIptal.Text = "Vazgeç";
            _btnIptal.Dock = DockStyle.Right;
            _btnIptal.Width = 120;
            _btnIptal.FlatStyle = FlatStyle.Flat;
            _btnIptal.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _lblOzet.Dock = DockStyle.Fill;
            _lblOzet.TextAlign = ContentAlignment.MiddleLeft;
            _lblOzet.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            alt.Controls.Add(_lblOzet);
            alt.Controls.Add(_btnIptal);
            alt.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 });
            alt.Controls.Add(_btnKopyala);

            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _grid.BackgroundColor = Color.White;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

            _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = ColSec, HeaderText = "Kopyala", Width = 60 });
            Kolon(ColSira, "Sıra", 45);
            Kolon(ColKod, "Kısa Kod", 150);
            Kolon(ColBrans, "Branş", 80);
            Kolon(ColHedef, "Hedef No", 65, duzenlenebilir: true);
            Kolon(ColCevap, "Cevap", 55, duzenlenebilir: true);
            Kolon(ColDurum, "Durum", 120);
            Kolon(ColSoruId, "Soru ID ✏️", 90, duzenlenebilir: true);
            Kolon(ColKaynakId, "Kaynak Klasör", 90);
            Kolon(ColDbCevap, "DB Cevap", 65);
            Kolon(ColZorluk, "Zorluk", 55);
            Kolon(ColYol, "Bulunan Kaynak", 420);
            Kolon(ColAciklama, "Açıklama", 400);
            Kolon(ColKazanim, "Kazanım", 300);

            _grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_grid.CurrentCell is DataGridViewCheckBoxCell) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += Grid_CellValueChanged;

            Controls.Add(_grid);
            Controls.Add(alt);
            Controls.Add(ust);
        }

        private static Button SecimButonu(string metin, Action tikla)
        {
            var b = new Button { Text = metin, AutoSize = true, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Margin = new Padding(0, 0, 8, 0) };
            b.Click += (s, e) => tikla();
            return b;
        }

        /// <summary>Kopyalanabilmesi için kaynak soru, kaynak klasör ID'si ve hedef soru no gerekir.</summary>
        private static bool SecilebilirMi(IsEmriSatiri s) =>
            s.Kaynak != null && !string.IsNullOrEmpty(s.Kaynak.SourceId) && !string.IsNullOrWhiteSpace(s.HedefSoruNo);

        private void TopluSec(Func<IsEmriSatiri, bool> kural)
        {
            _grid.EndEdit();
            _yukleniyor = true;
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.Tag is not IsEmriSatiri s) continue;
                s.Secili = kural(s) && SecilebilirMi(s);
                r.Cells[ColSec].Value = s.Secili;
            }
            _yukleniyor = false;
            OzetiGuncelle();
        }

        private void Kolon(string ad, string baslik, int genislik, bool duzenlenebilir = false)
        {
            var c = new DataGridViewTextBoxColumn { Name = ad, HeaderText = baslik, Width = genislik, ReadOnly = !duzenlenebilir, SortMode = DataGridViewColumnSortMode.NotSortable };
            if (duzenlenebilir) c.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 230);
            _grid.Columns.Add(c);
        }

        private void Doldur()
        {
            _yukleniyor = true;
            _grid.Rows.Clear();
            foreach (var s in _satirlar)
            {
                int i = _grid.Rows.Add();
                SatiriYaz(_grid.Rows[i], s);
            }
            _yukleniyor = false;
            OzetiGuncelle();
        }

        private void SatiriYaz(DataGridViewRow r, IsEmriSatiri s)
        {
            bool eski = _yukleniyor;
            _yukleniyor = true;

            r.Tag = s;
            r.Cells[ColSec].Value = s.Secili;
            r.Cells[ColSira].Value = s.SiraNo;
            r.Cells[ColKod].Value = s.KisaKod;
            r.Cells[ColBrans].Value = s.Brans;
            r.Cells[ColHedef].Value = s.HedefSoruNo;
            r.Cells[ColCevap].Value = s.Cevap;
            r.Cells[ColDurum].Value = DurumMetni(s.Durum);
            r.Cells[ColSoruId].Value = s.Kaynak?.SolutionId ?? "";
            r.Cells[ColKaynakId].Value = s.Kaynak?.SourceId ?? "";
            r.Cells[ColDbCevap].Value = s.Kaynak?.CevapAnahtari ?? "";
            r.Cells[ColYol].Value = s.Kaynak?.KisaYol ?? "";
            r.Cells[ColZorluk].Value = s.Kaynak?.Zorluk ?? "";
            r.Cells[ColKazanim].Value = s.Kaynak == null || s.Kaynak.KazanimId == "" ? "" : $"[{s.Kaynak.KazanimId}] {s.Kaynak.Kazanim}";
            r.Cells[ColAciklama].Value = s.Aciklama;

            Color renk = s.Durum switch
            {
                EslesmeDurumu.Kesin => Color.FromArgb(220, 245, 225),
                EslesmeDurumu.Duzeltme or EslesmeDurumu.ElleGirildi => Color.FromArgb(210, 232, 250),
                EslesmeDurumu.CevapFarkli or EslesmeDurumu.Coklu or EslesmeDurumu.KodAcilimCelisiyor or EslesmeDurumu.Tahmini or EslesmeDurumu.HedefNoBos => Color.FromArgb(255, 238, 200),
                _ => Color.FromArgb(252, 215, 215)
            };
            foreach (DataGridViewCell c in r.Cells)
            {
                if (!c.ReadOnly && c.OwningColumn.Name != ColSec) continue;
                c.Style.BackColor = renk;
            }

            _yukleniyor = eski;
        }

        private static string DurumMetni(EslesmeDurumu d) => d switch
        {
            EslesmeDurumu.Kesin => "✅ Kesin",
            EslesmeDurumu.Duzeltme => "📌 Düzeltmeden",
            EslesmeDurumu.ElleGirildi => "✏️ Elle girildi",
            EslesmeDurumu.CevapFarkli => "⚠️ Cevap farklı",
            EslesmeDurumu.Coklu => "⚠️ Birden fazla",
            EslesmeDurumu.KodAcilimCelisiyor => "⚠️ Kod/açılım farklı",
            EslesmeDurumu.Tahmini => "⚠️ Tahmini",
            EslesmeDurumu.HedefNoBos => "⏭️ Hedef no boş",
            _ => "❌ Bulunamadı"
        };

        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_yukleniyor || e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];
            if (row.Tag is not IsEmriSatiri s) return;
            string kolon = _grid.Columns[e.ColumnIndex].Name;
            string deger = (row.Cells[e.ColumnIndex].Value?.ToString() ?? "").Trim();

            if (kolon == ColSec)
            {
                bool secili = row.Cells[ColSec].Value is true;
                if (secili && !SecilebilirMi(s))
                {
                    MessageBox.Show("Bu satır seçilemez: kaynak soru, kaynak klasör ID'si veya hedef soru no eksik.\nÖnce 'Soru ID' veya 'Hedef No' hücresini doldurun.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    BeginInvoke(() => SatiriYaz(row, s));
                    return;
                }
                s.Secili = secili;
                OzetiGuncelle();
                return;
            }

            if (kolon == ColHedef) s.HedefSoruNo = deger;
            else if (kolon == ColCevap) s.Cevap = deger.ToUpperInvariant();
            else if (kolon == ColSoruId)
            {
                if (deger == "" || deger == s.Kaynak?.SolutionId) return;
                if (!ElleSoruIdGir(s, deger))
                {
                    BeginInvoke(() => SatiriYaz(row, s));
                    return;
                }
            }

            _eslestirici.Coz(s, _panel);
            BeginInvoke(() => { SatiriYaz(row, s); OzetiGuncelle(); });
        }

        /// <summary>Kullanıcının yazdığı Soru ID'yi doğrular ve kalıcı düzeltme olarak kaydeder.</summary>
        private bool ElleSoruIdGir(IsEmriSatiri s, string soruId)
        {
            if (!soruId.All(char.IsDigit))
            {
                MessageBox.Show("Soru ID sadece rakamlardan oluşmalı.", "Hatalı ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var dbSoru = _db.Indeks.IdIleBul(_panel, soruId);
            string kaynakId;

            if (dbSoru != null)
            {
                kaynakId = dbSoru.SourceId;
                var onay = MessageBox.Show(
                    $"[{s.KisaKod}] kodu bundan sonra HER ZAMAN şu soruya eşleşecek:\n\n" +
                    $"Soru ID: {dbSoru.SolutionId}\nKaynak: {dbSoru.KisaYol}\nKlasör ID: {kaynakId}\nCevap: {dbSoru.CevapAnahtari}\n\nKaydedilsin mi?",
                    "Kalıcı Düzeltme", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (onay != DialogResult.Yes) return false;
            }
            else
            {
                var baska = _db.Indeks.TumPanellerdeIdIleBul(soruId);
                if (baska != null)
                {
                    MessageBox.Show($"Bu Soru ID [{baska.Panel}] paneline ait, hedef panel ise [{_panel}]. Aynı panelden bir ID girin.", "Farklı Panel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                kaynakId = Microsoft.VisualBasic.Interaction.InputBox(
                    $"{soruId} numaralı soru [{_panel}] veritabanı Excel'lerinde yok.\n\nBu sorunun bulunduğu KAYNAK KLASÖR ID'sini girin:",
                    "Kaynak Klasör ID", "").Trim();
                if (kaynakId == "" || !kaynakId.All(char.IsDigit)) return false;
            }

            if (string.IsNullOrEmpty(kaynakId))
            {
                MessageBox.Show("Bu sorunun kaynak klasör ID'si veritabanında bilinmiyor.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _db.Duzeltmeler.Kaydet(_panel, s.KisaKod, soruId, kaynakId);
            return true;
        }

        private void OzetiGuncelle()
        {
            int secili = _satirlar.Count(x => x.Secili);
            int kesin = _satirlar.Count(x => x.Durum is EslesmeDurumu.Kesin or EslesmeDurumu.Duzeltme or EslesmeDurumu.ElleGirildi);
            int supheli = _satirlar.Count(x => x.Durum is EslesmeDurumu.CevapFarkli or EslesmeDurumu.Coklu or EslesmeDurumu.KodAcilimCelisiyor or EslesmeDurumu.Tahmini or EslesmeDurumu.HedefNoBos);
            int yok = _satirlar.Count(x => x.Durum == EslesmeDurumu.Bulunamadi);

            _lblOzet.Text = $"Toplam {_satirlar.Count}  |  ✅ Kesin: {kesin}  |  ⚠️ Kontrol gereken: {supheli}  |  ❌ Bulunamayan: {yok}  |  Kopyalanacak: {secili}";
            _lblOzet.ForeColor = supheli + yok == 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(211, 84, 0);
            _btnKopyala.Text = $"🚀 SEÇİLİ {secili} SORUYU KOPYALA";
        }
    }
}
