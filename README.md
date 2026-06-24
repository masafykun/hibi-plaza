# 🏙️ Hibi Plaza

> Unity で制作したオリジナルのリアルタイム 2.5D ソーシャル仮想世界

Hibi Plaza は Unity で制作したオリジナルの 2.5D ソーシャル仮想世界です。カラフルなアバターを作成し、広場を歩き回り、他の来訪者と出会い、チャットやエモートをリアルタイムで共有できます。

![Unity](https://img.shields.io/badge/Unity-6000.0.77f1-black?style=flat-square&logo=unity)
![WebGL](https://img.shields.io/badge/WebGL-Build-990000?style=flat-square&logo=webgl)
![Node.js](https://img.shields.io/badge/Node.js-22+-339933?style=flat-square&logo=nodedotjs)
![Blender](https://img.shields.io/badge/Blender-5.1.2-F5792A?style=flat-square&logo=blender)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)

🔗 **[Live Demo](https://masafykun.github.io/hibi-plaza/)**

### プレイ可能なバージョン

- [Version 01: Original Plaza](https://masafykun.github.io/hibi-plaza/versions/original/)
- [Version 02: Blender Edition](https://masafykun.github.io/hibi-plaza/versions/blender/)
- [Version 03: NPC Edition](https://masafykun.github.io/hibi-plaza/versions/npc/)
- [Version Archive](https://masafykun.github.io/hibi-plaza/versions/)

---

## 📸 スクリーンショット

![Hibi Plaza タイトル画面](Documentation/Screenshots/title.png)

![広場の来訪者とアニメーションする住人たち](Documentation/Screenshots/multiplayer.png)

![Kenney のカフェ家具と巡回する住人](Documentation/Screenshots/plaza-shops.png)

---

## 🎮 操作方法

| 操作 | 動作 |
|---|---|
| 歩く | 広場をクリック、または WASD キー |
| チャット | Enter キーで入力欄を開き、入力後 Enter で送信 |
| エモート | Wave・Cheer・Dance ボタンを使用 |
| 広場を離れる | Escape キー |

---

## ✨ 特徴

- **Blender 製モジュラーアバター** — 細かな顔、レイヤー化された髪、衣服、靴を備えたちびキャラアバター
- **アバタークリエイター** — 肌・髪色・4 種のヘアスタイル・トップス・ボトムスを選択可能
- **スタイライズドな広場** — 噴水、ショップ、カフェ家具、木々、街灯、ベンチ、花壇をモデリング
- **ローカル NPC 住人** — 独立した巡回ルートと吹き出しを持つ 4 体のアニメーション住人
- **CC0 アセット統合** — Kenney Mini Characters / Furniture Kit を住人と広場家具に採用
- **2 通りの移動** — クリック移動と WASD 移動に対応
- **リアルタイムマルチプレイ** — 位置と外見をリアルタイム同期
- **共有チャット** — 吹き出し付きの広場チャット
- **エモート** — Wave・Cheer・Dance の 3 種
- **オフラインプレビュー** — リアルタイムサーバー停止時も優雅にプレビュー表示
- **レスポンシブ WebGL** — カスタムローディング画面付きのレスポンシブ表示

---

## 🛠️ 技術スタック

| カテゴリ | 技術 |
|---|---|
| ゲームエンジン | Unity 6000.0.77f1 |
| 言語 | C# |
| 3D アセット制作 | Blender 5.1.2 |
| ビルド | WebGL |
| リアルタイムサーバー | Node.js 22+（WebSocket） |
| デプロイ | systemd / nginx |
| ホスティング | GitHub Pages |

---

## 📁 プロジェクト構成

- `Assets/HibiPlaza` — Unity ランタイム、エディタセットアップ、シェーダー、シーン、アートワーク
- `Assets/HibiPlaza/Resources/ThirdParty/Kenney` — 採用した CC0 ランタイムモデル
- `ArtSource/Blender` — 編集可能なアセットライブラリ
- `Tools/Blender` — 決定論的なアセット生成スクリプト
- `Assets/WebGLTemplates/HibiPlaza` — カスタム WebGL シェル
- `Server` — Node.js 製 WebSocket サービスと統合テスト
- `Deployment` — systemd と nginx の本番設定
- `docs` — GitHub Pages ビルド

---

## 🚀 セットアップ

```bash
# Unity 6000.0.77f1 でプロジェクトを開きます。
# エディタメニューの「Hibi Plaza」からプロジェクト設定、スモークテスト、WebGL ビルドを実行できます。

# 3D ライブラリは Blender 5.1.2 で作成されています。
# Tools/Blender/generate_hibi_assets.py を Blender のバックグラウンドモードで実行すると、
# アバターと最適化済み 8 種の FBX アセットグループ、ビジュアルプレビューを再生成できます。

# リアルタイムサーバーは Node.js 22 以降が必要です
cd Server
npm ci
npm test
npm start
```

ホスト版クライアントは `wss://hibi.160.251.234.247.nip.io/hibi` に接続します。サービスはメッセージ形式の検証、移動・チャット頻度の制限、マークアップと制御文字の除去、送信されたリンクの削除を行います。

---

## 📌 スコープ

本リポジトリはプレイ可能なバーティカルスライスであり、より大規模な仮想世界への基盤です。永続アカウント、ルーム、インベントリ、モデレーションツール、永続的なデータストレージは今後の自然なマイルストーンです。Hibi Plaza はオリジナルプロジェクトであり、Ameba Pigg や CyberAgent とは一切関係ありません。

---

## ライセンス

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

コードは **MIT ライセンス**（[LICENSE](LICENSE)）のもとで公開しています。Hibi Plaza のタイトルアートワークは本プロジェクトでの利用のために含まれています。サードパーティアセットの詳細は [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。

© 2026 masafykun (https://github.com/masafykun)
