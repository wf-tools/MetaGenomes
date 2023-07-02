## WHMetaSsetup  
whole-genome metagenome tool 
  
## なに？
Illumina Sequencer より出力されたショットガンシーケンスデータより
菌叢解析を行うツールです。


## ワークフロー
・Fastq トリミング/クオリティチェック  
  Trimmomatic  
  
・系統アサインメント  
  Centrifuge  
 
 ・系統アサインメント描画（html）  
  reCentrifuge  
  
  ・PCoA / nMDS  
  Python_PCoA 
  https://github.com/mwguthrie/python_PCoA  
  
  Permanova 
  https://github.com/theJohnnyBrown/permanova  

## 特徴  
Windows10/11 GUIにて操作し解析を実行  
結果出力はhtml 形式にてまとめられ、他のPCでも参照可能  


