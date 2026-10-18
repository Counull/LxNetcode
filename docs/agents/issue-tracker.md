# Issue tracker: GitHub

仓库：Counull/LxNetcode。

Issue、PRD 和决策票据保存在该仓库的 GitHub Issues。

优先使用当前提供的 GitHub 连接器；若使用 `gh` CLI，先确认其安装与登录状态。初始化时未在 PATH 中发现 `gh`。

- 发布票据：创建 GitHub Issue。
- 获取票据：读取正文、评论、标签和关联信息。
- 子任务与阻塞关系：优先使用平台原生关系；不可用时在正文明确记录。
- 远程写操作以当次用户请求或明确调用的工作流为准。

PRs as a request surface: no.

本配置不改变根目录 `AGENTS.md` 中的 AI 实现范围与设计确认要求。
