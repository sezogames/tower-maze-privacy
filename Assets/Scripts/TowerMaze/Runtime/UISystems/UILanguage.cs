using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerMaze
{
    internal static class UILanguage
    {
        internal const string DefaultCode = "EN";
        internal static readonly string[] SupportedCodes = { "EN", "TR", "ES" };
        private static readonly KeyValuePair<string, string>[] TurkishFixups =
        {
            new("Her gun sifirlanir", "Her gün sıfırlanır"),
            new("Gunluk", "Günlük"),
            new("GUNLUK", "GÜNLÜK"),
            new("Gunun", "Günün"),
            new("Gorevler", "Görevler"),
            new("GOREVLER", "GÖREVLER"),
            new("Gorev", "Görev"),
            new("GOREV", "GÖREV"),
            new("Hazirlaniyor", "Hazırlanıyor"),
            new("hazirlaniyor", "hazırlanıyor"),
            new("tamamlandi", "tamamlandı"),
            new("Tamamlandi", "Tamamlandı"),
            new("Titresim", "Titreşim"),
            new("TITRESIM", "TİTREŞİM"),
            new("Magaza", "Mağaza"),
            new("MAGAZA", "MAĞAZA"),
            new("Baglaniyor", "Bağlanıyor"),
            new("BAGLANIYOR", "BAĞLANIYOR"),
            new("Baglantisi", "Bağlantısı"),
            new("BAGLANTISI", "BAĞLANTISI"),
            new("Baska", "Başka"),
            new("BASKA", "BAŞKA"),
            new("BASKA BIR", "BAŞKA BİR"),
            new("Gecersiz", "Geçersiz"),
            new("GECERSIZ", "GEÇERSİZ"),
            new("Dondurmedi", "Döndürmedi"),
            new("DONDURMEDI", "DÖNDÜRMEDİ"),
            new("Urun", "Ürün"),
            new("URUN", "ÜRÜN"),
            new("Yukleniyor", "Yükleniyor"),
            new("YUKLENIYOR", "YÜKLENİYOR"),
            new("Yukleme", "Yükleme"),
            new("YUKLEME", "YÜKLEME"),
            new("Yuklenecek", "Yüklenecek"),
            new("YUKLENECEK", "YÜKLENECEK"),
            new("Yukseliyor", "Yükseliyor"),
            new("YUKSELIYOR", "YÜKSELİYOR"),
            new("Fiyatlari", "Fiyatları"),
            new("FIYATLARI", "FİYATLARI"),
            new("Aciliyor", "Açılıyor"),
            new("ACILIYOR", "AÇILIYOR"),
            new("Acik", "Açık"),
            new("ACIK", "AÇIK"),
            new("acik", "açık"),
            new("aciksa", "açıksa"),
            new("Aciksa", "Açıksa"),
            new("Odeme", "Ödeme"),
            new("ODEME", "ÖDEME"),
            new("Basla", "Başla"),
            new("BASLA", "BAŞLA"),
            new("Basarisiz", "Başarısız"),
            new("BASARISIZ", "BAŞARISIZ"),
            new("Dogrulamasi", "Doğrulaması"),
            new("DOGRULAMASI", "DOĞRULAMASI"),
            new("Islendi", "İşlendi"),
            new("ISLENDI", "İŞLENDİ"),
            new("Iptal", "İptal"),
            new("IPTAL", "İPTAL"),
            new("Yetersiz", "Yetersiz"),
            new("YETERSIZ", "YETERSİZ"),
            new("Yenilendi", "Yenilendi"),
            new("YENILENDI", "YENİLENDİ"),
            new("Geri Yukleme", "Geri Yükleme"),
            new("GERI YUKLEME", "GERİ YÜKLEME"),
            new("Geri Yukle", "Geri Yükle"),
            new("GERI YUKLE", "GERİ YÜKLE"),
            new("KONTROLU", "KONTROLÜ"),
            new("Kontrolu", "Kontrolü"),
            new("Odul", "Ödül"),
            new("ODUL", "ÖDÜL"),
            new("Odullu", "Ödüllü"),
            new("odullu", "ödüllü"),
            new("alindi", "alındı"),
            new("Alindi", "Alındı"),
            new("guncelle", "güncelle"),
            new("guncel", "güncel"),
            new("guncellenebilir", "güncellenebilir"),
            new("Guncellenebilir", "Güncellenebilir"),
            new("sifirla", "sıfırla"),
            new("sifirlan", "sıfırlan"),
            new("sifirlama", "sıfırlama"),
            new("sifirlamaniz", "sıfırlamanız"),
            new("kaldiril", "kaldırıl"),
            new("Kaldiril", "Kaldırıl"),
            new("uygulamayi", "uygulamayı"),
            new("kaydini", "kaydını"),
            new("yasadiginiz", "yaşadığınız"),
            new("onerilir", "önerilir"),
            new("sayfasindaki", "sayfasındaki"),
            new("icin", "için"),
            new("Icin", "İçin"),
            new("ICIN", "İÇİN"),
            new("gorunecek", "görünecek"),
            new("Gorunecek", "Görünecek"),
            new("kullanilamiyor", "kullanılamıyor"),
            new("Kullanilamiyor", "Kullanılamıyor"),
            new("KULLANILAMIYOR", "KULLANILAMIYOR"),
            new("secim", "seçim"),
            new("Secim", "Seçim"),
            new("acilmis", "açılmış"),
            new("Acilmis", "Açılmış"),
            new("Sinirli", "Sınırlı"),
            new("surum", "sürüm"),
            new("Suruyor", "Sürüyor"),
            new("SURUYOR", "SÜRÜYOR"),
            new("ulas", "ulaş"),
            new("Ulas", "Ulaş"),
            new("Bolge", "Bölge"),
            new("BOLGE", "BÖLGE"),
            new("ustunde", "üstünde"),
            new("Ustunde", "Üstünde"),
            new("devamsiz", "devamsız"),
            new("Devamsiz", "Devamsız"),
            new("yakin", "yakın"),
            new("Yakin", "Yakın"),
            new("kosu", "koşu"),
            new("Kosu", "Koşu"),
            new("KOSU", "KOŞU"),
            new("Tum", "Tüm"),
            new("TUM", "TÜM"),
            new("Birazdan", "Birazdan"),
            new("BIRAZDAN", "BİRAZDAN"),
            new("Coin ile ac", "Coin ile aç"),
            new("Gizlilik Politikasi", "Gizlilik Politikası"),
            new("Politikasi", "Politikası"),
            new("kaydi", "kaydı"),
            new("Kaydi", "Kaydı"),
            new("En Iyi", "En İyi"),
            new("EN IYI", "EN İYİ"),
            new("Aktif", "Aktif"),
            new("AKTIF", "AKTİF"),
            new("Geliyor", "Geliyor"),
            new("GELIYOR", "GELİYOR"),
            new("gorecek", "görecek")
        };

        internal static void EnsureDefaultLanguage()
        {
            if (PlayerPrefs.HasKey("Language"))
            {
                return;
            }

            PlayerPrefs.SetString("Language", DefaultCode);
            PlayerPrefs.Save();
        }

        internal static string GetLanguageCode()
        {
            string code = PlayerPrefs.GetString("Language", DefaultCode);
            if (string.IsNullOrWhiteSpace(code))
            {
                return DefaultCode;
            }

            code = code.Trim().ToUpperInvariant();
            for (int index = 0; index < SupportedCodes.Length; index++)
            {
                if (string.Equals(SupportedCodes[index], code, StringComparison.Ordinal))
                {
                    return code;
                }
            }

            return DefaultCode;
        }

        internal static void SetLanguageCode(string code)
        {
            string normalizedCode = NormalizeCode(code);
            PlayerPrefs.SetString("Language", normalizedCode);
            PlayerPrefs.Save();
        }

        internal static string Translate(string tr, string en, string es)
        {
            return GetLanguageCode() switch
            {
                "TR" => NormalizeTurkishText(tr),
                "ES" => es,
                _ => en,
            };
        }

        private static string NormalizeTurkishText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            for (int index = 0; index < TurkishFixups.Length; index++)
            {
                KeyValuePair<string, string> fixup = TurkishFixups[index];
                text = text.Replace(fixup.Key, fixup.Value);
            }

            return text;
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return DefaultCode;
            }

            code = code.Trim().ToUpperInvariant();
            for (int index = 0; index < SupportedCodes.Length; index++)
            {
                if (string.Equals(SupportedCodes[index], code, StringComparison.Ordinal))
                {
                    return code;
                }
            }

            return DefaultCode;
        }
    }
}
