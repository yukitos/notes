# Visual Studio 2015 RC

## FSharp

### 言語およびランタイム機能

* 第1級関数としてのコンストラクタ

  ````fsharp
  let urlStrings = [| "http://www1.example.com"; "http://www2.example.com" |]
  //let urls = urlStrings |> Array.map (fun s -> new Url(s))

  let urls = urlStrings |> Array.map Url
  ````

* `mutable`と`ref`の一元化

* 型プロバイダーのメソッドにおける静的パラメータのサポート

* 公開型における非null型のサポート

* メソッドの引数に対する暗黙的なクォート化

* プリプロセッサの文法強化

* 測定単位における有利指数のサポート

* `printf`系の関数に測定単位を渡す処理を簡素化

* .NETの多次元配列のサポート

* オブジェクト初期化子におけるプロパティのサポート

* 複数のジェネリックインターフェイスを実装するクラスからの継承

* `StructuredFormatDisplayAttribute`内で複数のプロパティを指定できる

* `Microsoft`で始まる名前空間の省略

### F#ランタイム

### F# IDE機能

