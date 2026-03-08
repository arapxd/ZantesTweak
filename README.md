# Zantes Tweak

Zantes Tweak is a branded Windows optimization suite focused on practical performance cleanup, profile-based tuning, live telemetry, benchmarking, Discord-backed session UX, and release-driven desktop delivery.

Zantes Tweak; pratik performans temizligi, profil tabanli tuning, canli telemetri, benchmark, Discord tabanli oturum deneyimi ve release odakli masaustu dagitimi icin hazirlanmis markali bir Windows optimizasyon paketidir.

## Download / Indirme

- Releases: [github.com/arapxd/ZantesTweak/releases](https://github.com/arapxd/ZantesTweak/releases)
- Repository: [github.com/arapxd/ZantesTweak](https://github.com/arapxd/ZantesTweak)

Public installers are shared through GitHub Releases.

Public kurulum dosyalari GitHub Releases uzerinden paylasilir.

## Core Features / Temel Ozellikler

- Dashboard-based system tuning flow
- Quick Boost and advanced optimizer profiles
- Live performance telemetry and readable guidance
- Benchmark capture with before/after comparison
- Discord session integration and Rich Presence
- GitHub Releases based update channel

- Dashboard tabanli sistem tuning akisi
- Quick Boost ve gelismis optimizer profilleri
- Canli performans telemetrisi ve anlasilir yonlendirme
- Once / sonra benchmark karsilastirmasi
- Discord oturum entegrasyonu ve Rich Presence
- GitHub Releases tabanli guncelleme kanali

## Modules / Moduller

- Dashboard
- Quick Boost
- Optimizer
- Network
- Performance Monitor
- Benchmark Lab
- Game Tuner

## Project Structure / Proje Yapisi

```text
ZantesEngine/               Main WPF application / Ana WPF uygulamasi
ZantesEngine/Pages/         UI pages / Arayuz sayfalari
ZantesEngine/Services/      Tweak, auth, telemetry, update, automation / Tweak, auth, telemetri, guncelleme, otomasyon
installer/                  Inno Setup installer script / Inno Setup kurulum scripti
scripts/                    Publish and packaging scripts / Publish ve paketleme scriptleri
docs/                       Release and repository notes / Release ve depo notlari
```

## Repository / Depo

- GitHub owner: `arapxd`
- GitHub repo: `ZantesTweak`
- Release page: `https://github.com/arapxd/ZantesTweak/releases`

## License / Lisans

This repository is public for visibility, not for reuse.

Bu depo gorunurluk icin public'tir, yeniden kullanim icin degildir.

- Source code is **proprietary**
- It is **not open source**
- Public access does **not** grant permission to copy, modify, redistribute, resell, or reuse the code
- See [LICENSE.txt](LICENSE.txt) and [EULA.txt](EULA.txt)

- Kaynak kod **proprietary** lisans altindadir
- **Acik kaynak degildir**
- Public gorunurluk; kopyalama, degistirme, dagitma, satma veya yeniden kullanma izni vermez
- Bkz. [LICENSE.txt](LICENSE.txt) ve [EULA.txt](EULA.txt)

If you want source code to remain both legally protected and invisible, the repository must be private.

Kaynak kodun hem hukuken korunmasi hem de gorunmemesi isteniyorsa deponun private olmasi gerekir.

## Update Channel / Guncelleme Kanali

The app checks the latest public release from GitHub Releases.

Uygulama en yeni public surumu GitHub Releases uzerinden kontrol eder.

- Config owner: `arapxd`
- Config repo: `ZantesTweak`
- Service: `ZantesEngine/Services/GitHubUpdateService.cs`
- Channel config: `ZantesEngine/Services/UpdateChannelConfig.cs`

No GitHub access token is stored in the app for update checks.

Guncelleme kontrolu icin uygulama icinde GitHub access token saklanmaz.

## Public Release / Public Dagitim

New public builds are expected to be published through the GitHub Releases page for this repository. Users should download installers from the Releases section instead of using repository source files directly.

Yeni public surumler bu deponun GitHub Releases sayfasi uzerinden yayinlanir. Kullanicilar kurulum dosyalarini kaynak kod yerine Releases bolumunden indirmelidir.

## Security Transparency / Guvenlik Seffafligi

Zantes Tweak may look suspicious to some antivirus engines because it is a Windows tweak utility that:

- requests administrator privileges for some operations
- changes registry and system configuration values
- runs Windows command-line tools such as `reg`, `netsh`, `powercfg`, and related system commands
- ships as a packaged desktop executable and installer

Zantes Tweak, bazi antivirus motorlarina supheli gorunebilir. Bunun temel nedeni uygulamanin:

- bazi islemler icin yonetici yetkisi istemesi
- registry ve sistem ayarlari uzerinde degisiklik yapmasi
- `reg`, `netsh`, `powercfg` gibi Windows sistem komutlarini kullanmasi
- paketlenmis masaustu uygulamasi ve installer olarak dagitilmasidir

During scanning, one or more heuristic or machine-learning based engines may flag the installer even when no real malicious payload is present. This can happen with optimization tools, low-level utilities, and unsigned Windows system tweakers.

Tarama sirasinda, heuristic veya machine-learning tabanli bazi motorlar gercek bir zararli yuk olmasa bile installer dosyasini isaretleyebilir. Bu durum optimizasyon araclarinda, dusuk seviye utility'lerde ve imzasiz Windows tweak uygulamalarinda gorulebilir.

## VirusTotal / VirusTotal Baglantisi

- Public VirusTotal report:
  [VirusTotal scan result](https://www.virustotal.com/gui/file/ec95a1160b080d7d68030e116520f1dee8a8ce66527d24b6b1b096f5b84649eb/detection)
- Local Windows Defender scan on the release installer completed with no threats found
- Public VirusTotal results may still show isolated heuristic detections from individual vendors
- If a release shows a detection from only one or a very small number of engines, it may be a false positive rather than a confirmed malicious verdict

- Public VirusTotal raporu:
  [VirusTotal tarama sonucu](https://www.virustotal.com/gui/file/ec95a1160b080d7d68030e116520f1dee8a8ce66527d24b6b1b096f5b84649eb/detection)
- Release installer dosyasi yerelde Windows Defender ile tarandi ve tehdit bulunmadi
- Public VirusTotal sonuclarinda tekil vendor bazli heuristic alarmlar gorulebilir
- Tek motor veya cok az sayida motor tarafindan gelen isaretleme, dogrulanmis zararli sonuc yerine false positive olabilir

Users who want to verify a release themselves should:

1. download only from the official GitHub Releases page
2. compare the published SHA256 hash with the downloaded file
3. review the public source repository
4. inspect VirusTotal results and especially the total number of detections, not just a single engine name

Kendi dogrulamasini yapmak isteyen kullanicilar:

1. dosyayi sadece resmi GitHub Releases sayfasindan indirmeli
2. yayimlanan SHA256 degerini indirilen dosya ile karsilastirmali
3. public kaynak kod deposunu incelemeli
4. VirusTotal sonucunda sadece tek motoru degil toplam detection sayisini da degerlendirmeli

## Notes / Notlar

- Local auth/session files, logs, build outputs, and release artifacts are ignored by Git through `.gitignore`
- Discord OAuth client secret is not embedded in the repository
- The repository is prepared for public release, but proprietary usage terms still apply

- Lokal auth/oturum dosyalari, loglar, build ciktilari ve release artefact'lari `.gitignore` ile repoya dahil edilmez
- Discord OAuth client secret repo icine gomulu degildir
- Depo public release icin hazirdir, ancak proprietary kullanim kosullari gecerliligini korur
