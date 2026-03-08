# Zantes Tweak

Zantes Tweak is a branded Windows optimization suite focused on practical performance cleanup, profile-based tuning, live telemetry, benchmarking, Discord-backed session UX, and release-driven desktop delivery.

The project is built as a WPF desktop app and shipped through versioned installers. Public releases are intended to be consumed through GitHub Releases and the in-app update channel.

## Turkce

Zantes Tweak; sistem optimizasyonu, canli performans izleme, benchmark, Discord oturum entegrasyonu ve surumlu masaustu dagitimi icin hazirlanmis bir Windows tweak uygulamasidir.

- Modern WPF arayuz
- Profil tabanli tweak ve temizlik akislari
- Canli telemetri ve benchmark karsilastirma
- GitHub Releases tabanli guncelleme sistemi
- Installer ve surumlu dagitim akisi

Indirme ve guncelleme dagitimi GitHub Releases uzerinden yapilir. Kurulum dosyalari release sayfasinda paylasilir.

Bu depo public olarak gorunur, ancak kodlar acik kaynak degildir. Kodun kopyalanmasi, yeniden kullanimi, dagitimi veya satilmasi lisans disidir.

## English

Zantes Tweak is a Windows optimization suite built for system tuning, live telemetry, benchmarking, Discord session integration, and versioned desktop delivery.

- Modern WPF interface
- Profile-based tweak and cleanup flows
- Live telemetry and benchmark comparison
- GitHub Releases based update delivery
- Installer-based distribution flow

Downloads and updates are delivered through GitHub Releases. Public installers are intended to be shared from the release page.

## Highlights

- Modern WPF interface with dashboard, benchmark, performance monitor, quick boost, optimizer, network, and game tuner flows
- Profile-driven tweak engine for system, network, latency, graphics, maintenance, and service cleanup
- Discord sign-in session flow with avatar sync and Discord Rich Presence support
- Live hardware telemetry, benchmark capture, before/after comparison, and user-readable performance guidance
- GitHub Releases based update channel without embedding any GitHub secret token inside the app
- Inno Setup based installer pipeline with versioned output and license screen

## Project Structure

```text
ZantesEngine/               Main WPF application
ZantesEngine/Pages/         UI pages
ZantesEngine/Services/      Tweak, auth, telemetry, update, and automation services
installer/                  Inno Setup installer script
scripts/                    Publish and packaging scripts
docs/                       Release and repository notes
```

## Repository

- GitHub owner: `arapxd`
- GitHub repo: `ZantesTweak`
- Release page: `https://github.com/arapxd/ZantesTweak/releases`

## License

This repository is public for visibility, not for reuse.

- Source code is **proprietary**
- It is **not open source**
- Public access does **not** grant permission to copy, modify, redistribute, resell, or reuse the code
- See [LICENSE.txt](LICENSE.txt) and [EULA.txt](EULA.txt)

If you want the code to remain legally protected while still visible, this is the intended model. If you want the code to be unreadable as well, the repository must be private.

## Update Channel

The app checks the latest public release from GitHub Releases.

- Config owner: `arapxd`
- Config repo: `ZantesTweak`
- Service: `ZantesEngine/Services/GitHubUpdateService.cs`
- Channel config: `ZantesEngine/Services/UpdateChannelConfig.cs`

No GitHub access token is stored in the app for update checks.

## Release Notes

New public builds are expected to be published through the GitHub Releases page for this repository. Users should download installers from the release section instead of using repository source files directly.

## Notes

- Local auth/session files, logs, build outputs, and release artifacts are ignored by Git through `.gitignore`
- Discord OAuth client secret is not embedded in the repository
- The repository is prepared for public release, but proprietary usage terms still apply
