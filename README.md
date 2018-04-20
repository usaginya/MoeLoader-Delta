# MoeLoader-Delta
> From https://github.com/esonic/moe-loader-v7

> From https://github.com/usaginya/MoeLoader-Delta

***

## About MoeLoader Δ

MoeLoader Δ 是以 MoeLoader 圖片瀏覽和收集工具延續的一個非官方編譯版本

此版本由 YIU 進行更新，AoiAizawa不定期繁體中文化，MoeLoader 原作者是 esonic

MoeLoader Δ 在 2016-12-25 誕生，至今修復了原程式大量BUG並增強了功能


GitHub: https://github.com/usaginya/MoeLoader-Delta

MoeLoader Δ 預覽：http://usaginya.lofter.com/post/1d56d69b_d6b14fd

[下載 MoeLoader Δ 更新\安裝 程式](https://raw.githubusercontent.com/usaginya/mkAppUpInfo/master/MoeLoader-Delta/Apps/MoeToNew.exe)



### 其它說明    Other

代理模式選擇

[使用IE代理] 可自動根據系統代理進行搜尋

[自訂] 代理地址格式為     代理IP:代理埠 

***

## Original Readme

MoeLoader是一個基於WPF的圖片瀏覽、收集工具。

MoeLoader官網(已失效): http://moeloader.sinaapp.com/

如何開發自訂站點：

1. 使用Visual Studio創建Class Library類型的項目，將項目屬性中的預設命名空間設定為SitePack；

2. 為項目添加引用，將MoeLoader資料夾中的MoeSite.dll添加到引用中；

3. 添加一個類，繼承MoeLoader.AbstractImageSite（假設該類命名為SiteSampleImgSite）；

4. 為SiteSampleImgSite類實現必選的SiteUrl、SiteName、ShortName（假設為sis）、GetImages、GetPageString屬性和方法；

5. 添加一個類，命名空間為SitePack，類聲明為public class SiteProvider，在該類中添加方法public List<MoeLoader.ImageSite> SiteList()，在該方法中返回含有SiteSampleImgSite類實例的List；

6. 在項目中添加一個資料夾，命名為image，在其中添加一個解析度為16*16的ico圖示檔案，重新命名為sis.ico（與上面設置的ShortName相同），在它的屬性中將Build Action設定為Embedded Resource；

7. 生成項目，將編譯好的類庫dll檔案重新命名為SitePackXXX.dll的形式（例如SitePackExt.dll），將重新命名後的dll檔案放到MoeLoader.exe所在的目錄下；

8. 執行MoeLoader，享受你新添加的站點！

PS. 關於MoeLoader介面中的AbstractImageSite、Img類詳細使用訊息，請參考MoeLoader原始碼中的注釋；

PS2. 若希望將自訂的站點加入MoeLoader正式版本中，請[與我聯繫](https://github.com/esonic)

下載自訂站點範例項目： https://code.google.com/p/moe-loader-v7/downloads/detail?name=SitePackSample.7z

------

### 支援的圖片站點：

* [yande.re](https://yande.re) (萌妹)
* [konachan.com](https://konachan.com)
* [danbooru.donmai.us](https://danbooru.donmai.us)
* [behoimi.org](http://behoimi.org) (三次元)
* [idol.sankakucomplex.com](https://idol.sankakucomplex.com) (三次元)(在 SitePacks 目錄中建立 18x.txt 可見)
* [chan.sankakucomplex.com](https://chan.sankakucomplex.com)
* [safebooru.org](http://safebooru.org)
* [gelbooru.com](https://gelbooru.com)
* [e-shuushuu.net](http://e-shuushuu.net)
* www.zerochan.net
* [mjv-art.org](https://anime-pictures.net)
* [worldcosplay.net](https://worldcosplay.net) (三次元)
* www.pixiv.net (標籤\完全標籤搜尋、畫師搜尋、日、周、月排行榜)
* www.minitokyo.net (桌面壁紙 、 掃描圖)
* [lolibooru.moe](https://lolibooru.moe) (蘿莉(需要檔案))
* [yuriimg.com](http://yuriimg.com) (百合)
* [atfbooru.ninja](http://atfbooru.ninja) (蘿莉(需要檔案))
* [rule34.xxx](https://rule34.xxx) (歐美多(需要檔案))
* [kawaiinyan.com](https://kawaiinyan.com)
