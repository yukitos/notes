# マネージWiX Bootstrapperを作成する

## 開発環境

### Visual Studio
Visual Studio 2012 Professional(VS2012)
### WiX 3.7

http://wix.codeplex.com/

wix37.exeでインストールして、Visual Studio用のテンプレートが使えるようにしておく

## ダミーインストーラープロジェクトの作成

1. [ファイル]-[新規作成]-[プロジェクト]を選択
2. [テンプレート]-[Windows Installer XML]-[Setup Project]を選択

```text
    名前: DummyInstaller (任意)
    場所: C:\work\ (任意)
    ソリューション名: CustomBootstrappter (任意)
```

### ダミーファイルの追加

1. ソリューションエクスプローラ上で「DummyInstaller」を選択後、右クリックから[追加]-[新しい項目]を選択
2. [Text File]を選択

```text
    名前: test.txt (任意)
```

中身はテストとわかるように適当な文字を書き込んでおく

### ダミーファイルをインストーラに組み込む

DummyInstallerプロジェクトのProduct.wxsファイルを編集して、追加したテキストファイルをインストーラに同梱する。

&lt;ComponentGroup Id="INSTALLFOLDER"&gt; ノード以下を変更：

```xml
    <Component Id="ProductComponent">
       <File Id="test.txt" Source="test.txt" Name="test.txt"/>
    <Component>
```

## Bootstrapper UXの作成

1. ソリューションエクスプローラー上で「ソリューション 'CustomBootstrapper'」を選択後、[追加]-[新しいプロジェクト]を選択
2. [インストール済み]-[Visual C#]-[クラス ライブラリ]を選択

```text
    名前：TestBA (任意)
    場所：C:\work\CustomBootstrapper (任意)
```

「Class.1.cs」ファイルは不要なので削除

// TODO:
