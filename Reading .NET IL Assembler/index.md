# Reading ".NET IL Assembler"

## ilのコンパイル方法

    > call "%vs120comntools%vsvars32.bat"
    > ilasm.exe myasm.il

* `.assembly extern` はアセンブリ参照を定義するもの。
  * 続くアセンブリ名にはファイル名を指定しない。
    * `mscorlib.dll` と指定してしまうと `mscorlib.dll.dll` だとか `mscorlib.dll.exe` が検索されてしまうので
      見つからずランタイムエラーになる。
  * `{ auto }` とするとバージョンを自動検出する。
    * ただし `1.0` と `1.1` には自動検出機能がないので注意。
* `.assembly <name>` で`Assembly`という名前のメタデータを定義。
* `.module <name>` で`Module`という名前のメタデータを定義。
