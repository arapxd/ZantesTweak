# GitHub Release Checklist

- `ZantesEngine/Services/UpdateChannelConfig.cs` icindeki placeholder degerleri kendi GitHub `owner/repo` bilginle degistir veya release ortaminda `ZANTES_GITHUB_OWNER` ve `ZANTES_GITHUB_REPO` environment variable'larini tanimla.
- Discord tarafinda `client secret` kullanma. Mevcut akista sadece public `client id` var; secret gerektiren akis eklenirse koda gomulmemeli.
- `.gitignore` sayesinde `bin/`, `obj/`, `release/`, loglar ve lokal auth/settings dosyalari repoya gitmez. Commit oncesi yine de `git status` ile kontrol et.
- Lokal token dosyalari `%LocalAppData%\\ZantesEngine` altinda tutulur; repo icinde degildir.
- GitHub updater sadece public `releases/latest` endpoint'ini okur; uygulama icinde GitHub access token saklanmaz.
- Release tag formatini `v1.0.0` veya `1.0.0` olarak tut. Uygulama ve installer surumu `ZantesEngine.csproj` icindeki `<Version>` alanindan gelir.
- Installer build alirken `scripts/build-release-package.ps1` kullan. Script csproj versiyonunu installer ismine de tasir.
