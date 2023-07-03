# Shotgun Metagenome tool

**これは何？**

高速シーケンサより出力されたショットガン・シーケンス出力Fastq から

菌叢解析を行うツールです。

結果出力は、CSV形式ファイル、サンバースト図などがあります。



インプットされたFastqファイルをKRAKEN2のデータベースにて分類し

Recentrifugeを利用したサンバースト図を作成します。

サンバースト図はhtml形式で出力されローカル環境で操作が可能です。



-- インストール方法

以下のURLからWindowsインストーラをダウンロードしてインストールします。

インストーラは分類データベースを同梱しているため7GBのファイルになります。

https://drive.google.com/file/d/13pPkxETPr3wEtC8EYEkyMis1h_PlkTI8/view?usp=sharing



-- 操作

起動画面ににFastqファイルをドラッグ&ドロップにて指定します。

（この時グルーピング情報を追加します）。

分類する対象データベースを選択し、出力フォルダを指定すると

実行が開始されます（とても簡単！！）。





-- ワークフロー

1. ・trimmomatic によるアダプタートリムとQC

   http://www.usadellab.org/cms/?page=trimmomatic

   

2. ・KRAKEN2 によるClassification

   http://ccb.jhu.edu/software/kraken2/index.shtml

   

3. ・Recentrifuge によるvisualize

   https://github.com/khyox/recentrifuge





-- 追加

Fastqサンプルが3グループ以上の場合、nMDSとPCoA を

行います。結果はhtml形式ファイルにて参照します。



  Python_PCoA /nMDS
  https://github.com/mwguthrie/python_PCoA  

  Permanova 
  https://github.com/theJohnnyBrown/permanova  



## 特徴  

Windows10/11 GUIにて操作し解析を行えます。  
結果出力はhtml 形式にてまとめられ、他のPCでも参照可能 です。

（出力フォルダごとコピーします）