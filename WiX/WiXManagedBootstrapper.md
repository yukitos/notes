# マネージWiX Bootstrapperを作成する

参照記事： http://bryanpjohnston.com/2012/09/28/custom-wix-managed-bootstrapper-application

Livetを使っていること以外は上の方の手順そのまま。

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
    名前: DummyInstaller
    場所: C:\work\
    ソリューション名: CustomBootstrappter
```

### ダミーファイルの追加

1. ソリューションエクスプローラ上で「DummyInstaller」を選択後、右クリックから[追加]-[新しい項目]を選択
2. [Text File]を選択

```text
    名前: test.txt
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

### Manufacturerの設定

ProductノードのManufacturer属性はデフォルトで空文字になっているので、適宜変更する

## Bootstrapper UXの作成

1. ソリューションエクスプローラー上で「ソリューション 'CustomBootstrapper'」を選択後、[追加]-[新しいプロジェクト]を選択
2. [インストール済み]-[Visual C#]-[クラス ライブラリ]を選択

```text
    名前：TestBA
    場所：C:\work\CustomBootstrapper
```

「Class.1.cs」ファイルは不要なので削除

### AssemblyInfo.csの変更

AssemblyInfo.csファイルに以下を追加(usingはファイルの先頭に追加する必要がある点に注意)

```csharp
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
    [assembly: BootstrapperApplication(typeof(TestBA.TestBA))]
```

### Livetの組み込み

1. ソリューションエクスプローラー上で「TestBA」プロジェクトを選択後、右クリックして「NuGet パッケージの管理」を選択
2. 「オンライン」を選択後、「Livet」で検索
3. 「Livet Cask」をインストール
4. 「NuGet パッケージの管理」ダイアログを閉じる

### 参照設定

1. 「参照の追加」ダイアログで「参照」ボタンを押して、WiXのSDKフォルダ(ex. C:\Program Files (x86)\WiX Toolset v3.7\SDK)内にある以下のファイルを選択

* BootstrapperCore.dll
* Microsoft.Deployment.WindowsInstaller.dll

### ViewModelの作成

1. ソリューションエクスプローラー上で「TestBA」プロジェクトを選択後、右クリックして[追加]-[クラス]を選択
2. 「MainViewModel.cs」ファイルを追加
3. 以下の通り実装する：

```csharp
    using System;
    using System.Collections.Generic;
    using Livet;
    using Livet.Commands;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

    namespace TestBA
    {
        public class MainViewModel : ViewModel
        {
            public MainViewModel(BootstrapperApplication bootstrapper)
            {
                IsThinking = false;
                Bootstrapper = bootstrapper;
                Bootstrapper.ApplyComplete += OnApplyComplete;
                Bootstrapper.DetectPackageComplete += OnDetectPackageComplete;
                Bootstrapper.PlanComplete += OnPlanComplete;
            }

            private bool _installEnabled;
            public bool InstallEnabled
            {
                get { return _installEnabled; }
                set
                {
                    if (EqualityComparer<bool>.Default.Equals(_installEnabled, value))
                        return;
                    _installEnabled = value;
                    RaisePropertyChanged();
                }
            }

            private bool _uninstallEnabled;
            public bool UninstallEnabled
            {
                get { return _uninstallEnabled; }
                set
                {
                    if (EqualityComparer<bool>.Default.Equals(_uninstallEnabled, value))
                        return;
                    _uninstallEnabled = value;
                    RaisePropertyChanged();
                }
            }

            private bool _isThinking;
            public bool IsThinking
            {
                get { return _isThinking; }
                set
                {
                    if (EqualityComparer<bool>.Default.Equals(_isThinking, value))
                        return;
                    _isThinking = value;
                    RaisePropertyChanged();
                }
            }

            public BootstrapperApplication Bootstrapper { get; private set; }

            private ViewModelCommand _installCommand;
            public ViewModelCommand InstallCommand
            {
                get
                {
                    if (_installCommand == null)
                        _installCommand = new ViewModelCommand(Install, CanInstall);
                    return _installCommand;
                }
            }

            public void Install()
            {
                IsThinking = true;
                Bootstrapper.Engine.Plan(LaunchAction.Install);
            }

            public bool CanInstall()
            {
                return InstallEnabled;
            }

            private ViewModelCommand _uninstallCommand;
            public ViewModelCommand UninstallCommand
            {
                get
                {
                    if (_uninstallCommand == null)
                        _uninstallCommand = new ViewModelCommand(Uninstall, CanUninstall);
                    return _uninstallCommand;
                }
            }


            public void Uninstall()
            {
                IsThinking = true;
                Bootstrapper.Engine.Plan(LaunchAction.Uninstall);
            }

            public bool CanUninstall()
            {
                return UninstallEnabled;
            }

            private ViewModelCommand _exitCommand;
            public ViewModelCommand ExitCommand
            {
                get
                {
                    if (_exitCommand == null)
                        _exitCommand = new ViewModelCommand(Exit);
                    return _exitCommand;
                }
            }

            public void Exit()
            {
                TestBA.BootstrapperDispatcher.InvokeShutdown();
            }

            private void OnApplyComplete(object sender, ApplyCompleteEventArgs e)
            {
                IsThinking = false;
                InstallEnabled = false;
                UninstallEnabled = false;
            }

            private void OnDetectPackageComplete(object sender, DetectPackageCompleteEventArgs e)
            {
                if (e.PackageId == "DummyInstallationPackageId")
                {
                    if (e.State == PackageState.Absent)
                        InstallEnabled = true;
                    else if (e.State == PackageState.Present)
                        UninstallEnabled = true;
                }
            }

            private void OnPlanComplete(object sender, PlanCompleteEventArgs e)
            {
                if (e.Status >= 0)
                    Bootstrapper.Engine.Apply(IntPtr.Zero);
            }
        }
    }

```

なおこの時点ではExit()メソッドで呼び出しているstaticプロパティTestBA.BootstrapperDispatcherが未定義なのでエラーになる点に注意。

### Viewの作成

1. ソリューションエクスプローラー上で「TestBA」プロジェクトを選択後、右クリックして[追加]-[新しい項目]を選択
2. 「ユーザーコントロール (WPF)」を選択

```text
    名前：MainView.xaml
```

#### xamlファイルの変更

追加した直後はルートノードが「UserControl」になっているので「Window」に変更する。

```xml
    <Window x:Class="TestBA.MainView"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
                 xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
                 mc:Ignorable="d" 
                 Width="400" MinWidth="400" Height="400" MinHeight="400"
                 Title="Simple Bootstrapper Application">
        <Window.Resources>
            <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
        </Window.Resources>
        <Grid>
            <TextBlock Text="Welcome to my test bootstrapper application." Margin="10" FontSize="10" HorizontalAlignment="Center"/>
            <Ellipse Height="100" Width="100" HorizontalAlignment="Center" VerticalAlignment="Center" StrokeThickness="6" Margin="10"
                     Visibility="{Binding Path=IsThinking, Converter={StaticResource BooleanToVisibilityConverter}}">
                <Ellipse.Stroke>
                    <LinearGradientBrush>
                        <GradientStop Color="Red" Offset="0.0"/>
                        <GradientStop Color="White" Offset="0.9"/>
                    </LinearGradientBrush>
                </Ellipse.Stroke>
                <Ellipse.RenderTransform>
                    <RotateTransform x:Name="Rotator" CenterX="50" CenterY="50" Angle="0"/>
                </Ellipse.RenderTransform>
                <Ellipse.Triggers>
                    <EventTrigger RoutedEvent="Ellipse.Loaded">
                        <BeginStoryboard>
                            <Storyboard TargetName="Rotator" TargetProperty="Angle">
                                <DoubleAnimation By="360" Duration="0:0:2" RepeatBehavior="Forever"/>
                            </Storyboard>
                        </BeginStoryboard>
                    </EventTrigger>
                </Ellipse.Triggers>
            </Ellipse>
            <StackPanel Orientation="Horizontal" VerticalAlignment="Bottom" HorizontalAlignment="Right">
                <Button Content="Install" Command="{Binding InstallCommand}"
                        Visibility="{Binding InstallEnabled, Converter={StaticResource BooleanToVisibilityConverter}}"
                        Margin="10" Height="20" Width="80"/>
                <Button Content="Uninstall" Command="{Binding UninstallCommand}"
                        Visibility="{Binding UninstallEnabled, Converter={StaticResource BooleanToVisibilityConverter}}"
                        Margin="10" Height="20" Width="80"/>
                <Button Content="Exit" Command="{Binding ExitCommand}" Margin="10" Height="20" Width="80"/>
            </StackPanel>
        </Grid>
    </Window>

```

#### コードビハインドの変更

変更点は1カ所、親クラスを「UserControl」から「Window」に変更する。

### BootstrapperApplicationの実装

1. ソリューションエクスプローラー上で「TestBA」プロジェクトを選択後、右クリックして[追加]-[クラス]を選択
2. 「TestBA.cs」ファイルを追加

```csharp
    using System.Windows.Threading;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

    namespace TestBA
    {
        public class TestBA : BootstrapperApplication
        {
            public static Dispatcher BootstrapperDispatcher { get; private set; }

            protected override void Run()
            {
                Engine.Log(LogLevel.Verbose, "Launching custom TestBA UX");

                BootstrapperDispatcher = Dispatcher.CurrentDispatcher;

                var viewModel = new MainViewModel(this);
                viewModel.Bootstrapper.Engine.Detect();

                var view = new MainView();
                view.DataContext = viewModel;
                view.Closed += (sender, e) => BootstrapperDispatcher.InvokeShutdown();
                view.Show();
                Dispatcher.Run();

                Engine.Quit(0);
            }
        }
    }
```

### Bootstrapper用設定ファイルの追加

1. ソリューションエクスプローラー上で「TestBA」プロジェクトを選択後、右クリックして[追加]-[新しい項目]を選択
2. [インストール済み]-[Visual C# アイテム]にある[XML ファイル]を選択

```text
    名前：BootstrapperCore.config
```

#### 設定ファイルの変更

BootstrapperCore.configファイルを以下の通り変更する。

```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <configuration>
        <configSections>
            <sectionGroup name="wix.bootstrapper" type="Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperSectionGroup, BootstrapperCore">
                <section name="host" type="Microsoft.Tools.WindowsInstallerXml.Bootstrapper.HostSection, BootstrapperCore" />
            </sectionGroup>
        </configSections>
        <startup useLegacyV2RuntimeActivationPolicy="true">
            <supportedRuntime version="v4.0"/>
        </startup>
        <wix.bootstrapper>
            <host assemblyName="TestBA">
                <supportedFramework version="v4\Full"/>
                <supportedFramework version="v4\Client"/>
            </host>
        </wix.bootstrapper>
    </configuration>
```

## Bootstrapper Projectの追加

1. ソリューションエクスプローラー上で「ソリューション 'CustomBootstrapper'」を選択後、[追加]-[新しいプロジェクト]を選択
2. [インストール済み]-[Windows Installer XML]-[Bootstrapper Project]を選択

```text
    名前：BootstrapperSetup
```

### プロジェクト参照の設定

1. ソリューションエクスプローラー上で[BootstrapperSetup]-[References]を選択後、右クリックから[参照の追加]を選択
2. デフォルトで[参照]タブが選択されていて、WiXのインストールフォルダが選択された状態になっているので、「WixUtilExtension.dll」を選択して[追加]を押す
3. [プロジェクト]タブを選択後、「TestBA」プロジェクトを選択して[追加]を押す

### Bundle.wxsの変更

```xml
    <?xml version="1.0" encoding="UTF-8"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi"
         xmlns:util="http://schemas.microsoft.com/wix/UtilExtension">
      <Bundle Name="BootstrapperSetup" Version="1.0.0.0" Manufacturer="Private" UpgradeCode="27a4f806-db94-478a-b639-be690c45e390">
        <BootstrapperApplicationRef Id="ManagedBootstrapperApplicationHost">
          <Payload SourceFile="..\TestBA\BootstrapperCore.config"/>
          <Payload SourceFile="..\TestBA\bin\$(var.Configuration)\TestBA.dll"/>
          <Payload SourceFile="..\TestBA\bin\$(var.Configuration)\Microsoft.Deployment.WindowsInstaller.dll"/>
          <Payload SourceFile="..\TestBA\bin\$(var.Configuration)\Livet.dll"/>
        </BootstrapperApplicationRef>

        <Chain>
          <PackageGroupRef Id="Netfx4Full"/>
          <MsiPackage SourceFile="..\DummyInstaller\bin\$(var.Configuration)\DummyInstaller.msi"
                      Id="DummyInstallationPackageId" Cache="yes" Visible="no"/>
        </Chain>
      </Bundle>
      <Fragment>
        <WixVariable Id="WixMbaPrereqPackageId" Value="Netfx4Full"/>
        <WixVariable Id="WixMbaPrereqLicenseUrl" Value="NetfxLicense.rtf"/>
        <util:RegistrySearch Root="HKLM" Key="SOFTWARE\Microsoft\Net Framework Setup\NDP\v4\Full" Value="Version" Variable="Netfx4FullVersion"/>
        <util:RegistrySearch Root="HKLM" Key="SOFTWARE\Microsoft\Net Framework Setup\NDP\v4\Full" Value="Version" Variable="Netfx4x64FullVersion" Win64="yes"/>
        <PackageGroup Id="Netfx4Full">
          <ExePackage Id="Netfx4Full" Cache="no" Compressed="yes" PerMachine="yes" Permanent="yes" Vital="yes"
                      SourceFile="C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bootstrapper\Packages\DotNetFX40\dotNetFx40_Full_x86_x64.exe"
                      DetectCondition="Netfx4FullVersion AND (NOT VersionNT64 OR Netfx4x64FullVersion)"/>
        </PackageGroup>
      </Fragment>
    </Wix>
```

### BootstrapperSetupをビルド

## 動作確認

起動直後：

![alt text][img01]

インストール中：

![alt text][img02]

インストール後：

![alt text][img03]

既にインストールされた状態で起動した直後：

![alt text][img04]

アンインストール中：

![alt text][img05]

アンインストール後：

![alt text][img06]

[img01]: 01.png "Startup"
[img02]: 02.png "During installation"
[img03]: 03.png "After installation"
[img04]: 04.png "Startup when installed"
[img05]: 05.png "During uninstallation"
[img06]: 06.png "After uninstallation"
