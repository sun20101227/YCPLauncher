=================================================
YCPNameSync - CS2 服务端改名同步插件
=================================================

【目录结构】
- YCPNameSync.cs : 插件源码，可供查阅修改
- bin\Release\Output\YCPNameSync.dll : 已编译好的插件文件（可以直接丢到服务器里用）

【安装说明】
1. 确保您的 CS2 游戏服务器已经安装了 Metamod:Source 和 CounterStrikeSharp 框架。
2. 进入您服务器的目录：`csgo/addons/counterstrikesharp/plugins/`
3. 在这个目录下新建一个文件夹，命名为 `YCPNameSync`
4. 将本压缩包 `bin\Release\Output\` 目录下的所有 `.dll` 文件扔进这个 `YCPNameSync` 文件夹里。
5. 重启 CS2 游戏服务器，或者在服务器控制台输入 `css_plugins reload YCPNameSync`。

【原理说明】
由于 CS2 默认锁死玩家在计分板上的名字为 Steam 名称，单纯靠启动参数 `-name` 是无效的。
现在的方案是：
1. YCPLauncher (客户端) 会在启动参数中秘密带上一句：`+setinfo ycp_name "玩家登录名"`
2. 您刚部署好的这个 YCPNameSync (服务端插件) 会拦截玩家的连接。
3. 如果玩家传了 `ycp_name`，插件就会在玩家进服的一瞬间，调用底层 API 强行把他的计分板名字覆盖为您平台上的名字！

如果您有任何疑问，或者控制台报错，请随时反馈给 AI 助手（Antigravity）。
