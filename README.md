# MoeLoader-Delta
> From https://github.com/esonic/moe-loader-v7

***

## About MoeLoader Δ

MoeLoader Δ 是以 MoeLoader 图片浏览和收集工具延续的一个非官方编译版本

此版本由 YIU 进行更新，MoeLoader 原作者是 esonic

MoeLoader Δ 在 2016-12-25 诞生，至今修复了原程序大量BUG并增强了功能


GitHub: https://github.com/usaginya/MoeLoader-Delta

MoeLoader Δ 预览：http://usaginya.lofter.com/post/1d56d69b_d6b14fd

[下载 MoeLoader Δ 更新\安装 程序](https://raw.githubusercontent.com/usaginya/mkAppUpInfo/master/MoeLoader-Delta/Apps/MoeToNew.exe)



### 其它说明    Other

代理模式选择

[使用IE代理] 可自动根据系统代理进行搜索

[自定义] 代理地址格式为     代理IP:代理端口 

***

## Original Readme

MoeLoader是一个基于WPF的图片浏览、收集工具。

MoeLoader官网(已失效): http://moeloader.sinaapp.com/

如何开发自定义站点：

1. 使用Visual Studio创建Class Library类型的项目，将项目属性中的默认命名空间设置为SitePack；

2. 为项目添加引用，将MoeLoader文件夹中的MoeSite.dll添加到引用中；

3. 添加一个类，继承MoeLoader.AbstractImageSite（假设该类命名为SiteSampleImgSite）；

4. 为SiteSampleImgSite类实现必选的SiteUrl、SiteName、ShortName（假设为sis）、GetImages、GetPageString属性和方法；

5. 添加一个类，命名空间为SitePack，类声明为public class SiteProvider，在该类中添加方法public List<MoeLoader.ImageSite> SiteList()，在该方法中返回含有SiteSampleImgSite类实例的List；

6. 在项目中添加一个文件夹，命名为image，在其中添加一个分辨率为16*16的ico图标文件，重命名为sis.ico（与上面设置的ShortName相同），在它的属性中将Build Action设置为Embedded Resource；

7. 生成项目，将编译好的类库dll文件重命名为SitePackXXX.dll的形式（例如SitePackExt.dll），将重命名后的dll文件放到MoeLoader.exe所在的目录下；

8. 运行MoeLoader，享受你新添加的站点！

PS. 关于MoeLoader接口中的AbstractImageSite、Img类详细使用信息，请参考MoeLoader源代码中的注释；

PS2. 若希望将自定义的站点加入MoeLoader正式版本中，请[与我联系](https://github.com/esonic)

下载自定义站点示例项目： https://code.google.com/p/moe-loader-v7/downloads/detail?name=SitePackSample.7z

------

### 支持的图片站点：

* [yande.re](https://yande.re) (萌妹)
* [konachan.com](https://konachan.com)
* [danbooru.donmai.us](https://danbooru.donmai.us)
* [behoimi.org](http://behoimi.org) (三次元)
* [idol.sankakucomplex.com](https://idol.sankakucomplex.com) (三次元)(在 SitePacks 目录中新建 18x.txt 可见)
* [chan.sankakucomplex.com](https://chan.sankakucomplex.com)
* [safebooru.org](http://safebooru.org)
* [gelbooru.com](https://gelbooru.com)
* [e-shuushuu.net](http://e-shuushuu.net)
* www.zerochan.net
* [mjv-art.org](https://anime-pictures.net)
* [worldcosplay.net](https://worldcosplay.net) (三次元)
* www.pixiv.net (标签\完全标签搜索、画师搜索、日、周、月排行榜)
* www.minitokyo.net (桌面壁纸 、 扫描图)
* [lolibooru.moe](https://lolibooru.moe) (萝莉(需要文件))
* [yuriimg.com](http://yuriimg.com) (百合)
* [atfbooru.ninja](http://atfbooru.ninja) (萝莉(需要文件))
* [rule34.xxx](https://rule34.xxx) (欧美多(需要文件))
* [kawaiinyan.com](https://kawaiinyan.com)