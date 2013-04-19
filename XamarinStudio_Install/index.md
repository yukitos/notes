# Memo about Xamarin Studio Install

## Xamarin Studioのダウンロード

<http://xamarin.com/download> にアクセスして名前、メールアドレス、国名、ロールを入力または選択後、ダウンロードを開始する。

![Xamarin Studio Download Page][img01]

ロールは以下のいずれかから選択する。今回は個人用にインストールしてみるだけなので「Indie」を選択。

* Academic
  * 学術機関に所属する学生または教員
* Indie
  * 個人または同人活動を行うプログラマ
* Professional
  * 会社や組織に属するプログラマ

入力後[Download]を押してしばらく待つとXamarinInstaller.exeがダウンロードできる。

![Xamarin Studio Downloading][img02]

## Xamarin Studioのインストール

### ダウンロードしたXamarinInstaller.exeを実行する

![Xamarin Studio Install 01][img03]

![Xamarin Studio Install 02][img04]

![Xamarin Studio Install 03][img05]

![Xamarin Studio Install 04][img06]

![Xamarin Studio Install 05][img07]

![Xamarin Studio Install 06][img08]

![Xamarin Studio Install 07][img09]

![Xamarin Studio Install 08][img10]

![Xamarin Studio Install 09][img11]

## Xamarin Studioの起動

[スタート]-[すべてのプログラム]-[Xamarin Studio]で起動する。

![Xamarin Studio Startup][img12]

## Xamarin StudioにF#の開発環境をインストールする

[ツール]-[アドイン マネージャ]を選択

![Xamarin Studio Create New Solution][img13]

[Gallery]タブを選択した状態で「fsharp」を検索。

![Xamarin Studio F# Add-ins][img14]

「F# support for Xamarin.Android development」を選択してインストール（すれば自動的に依存関係が解決されてF# Language Bindingもインストールされる）。

## Xamarin StudioでF#アプリケーションを作成する

F#のアドインをインストールすると以下のようなプロジェクトテンプレートが追加される。

![Xamarin Studio F# Projects][img15]

![Xamarin Studio F# for Android Projects][img16]

![Xamarin Studio F# for ASP.NET Projects][img17]

### F# 3.0 Console Projectの作成

[ファイル]-[新規]-[ソリューション]を選択後、[F#]-[F# 3.0 Console Project]を選択して適当な名前とソリューション名を入力する。

![Xamarin Studio New Solution Dialog][img18]

ファイルを追加するにはソリューションエクスプローラ上でプロジェクトを右クリックしてから[追加]-[新しいファイル]を選択する。

![Xamarin Studio New File Dialog][img19]

ファイルを追加した後はファイルのビルド順序を指定する必要がある。
ビルド順序はメニューから[プロジェクト]-[(プロジェクト名)のオプション]を選択し、[ビルド]-[一般]の[Build Order]タブで指定する。

![Xamarin Studio F# Project's Build Order][img20]

[img01]: <img/img01.png> "Download page"
[img02]: <img/img02.png> "Downloading"
[img03]: <img/img03.png> "Xamarin Installer 01"
[img04]: <img/img04.png> "Xamarin Installer 02"
[img05]: <img/img05.png> "Xamarin Installer 03"
[img06]: <img/img06.png> "Xamarin Installer 04"
[img07]: <img/img07.png> "Xamarin Installer 05"
[img08]: <img/img08.png> "Xamarin Installer 06"
[img09]: <img/img09.png> "Xamarin Installer 07"
[img10]: <img/img10.png> "Xamarin Installer 08"
[img11]: <img/img11.png> "Xamarin Installer 09"
[img12]: <img/img12.png> "Xamarin Studio Startup"
[img13]: <img/img13.png> "Xamarin Studio Add-in Manager"
[img14]: <img/img14.png> "Xamarin Studio F# Add-ins"
[img15]: <img/img15.png> "Xamarin Studio F# Projects"
[img16]: <img/img16.png> "Xamarin Studio F# for Android Projects"
[img17]: <img/img17.png> "Xamarin Studio F# for ASP.NET Projects"
[img18]: <img/img18.png> "Xamarin Studio F# 3.0 Console Project"
[img19]: <img/img19.png> "Xamarin Studio Adding New Class File"
[img20]: <img/img20.png> "Xamarin Studio F# Project's Build Order"
