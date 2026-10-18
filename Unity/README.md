# Unity 开发与验证工程

此目录预留给 Unity 开发与验证工程，目前不是可打开的 Unity 工程。

创建工程前先确认编辑器版本；创建后记录实际 `ProjectVersion.txt`、API Compatibility Level、构建目标和脚本后端。当前不生成或猜测 `Assets/`、`ProjectSettings/` 和工程包清单。

工程将消费仓库中 `Packages/com.counull.lxnetcode/` 的同一份源码，用于本地开发、loopback 测试和集成验证。实际游戏项目以后通过 Git URL 安装该 UPM 包。

工程源码、Unity `.meta`、项目配置、包 manifest 与锁文件纳入 Git；Library、Temp、Logs 等生成数据由根目录 `.gitignore` 排除。

组织方式见[组织决策](../docs/ARCHITECTURE.md)，当前阶段见[项目状态](../docs/STATUS.md)。
