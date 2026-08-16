# Sts2CharacterModel

无外部 NuGet 依赖的 .NET 9 源码审计与数值附件生成器。它直接引用本地 `sts2.csproj`，以游戏的卡牌升级实现和 `StandardActMap` 为真值。

```powershell
dotnet run --project analysis/Sts2CharacterModel -- build `
  --source D:\Work\sts2\code `
  --character Ironclad `
  --ascension 10 `
  --seed 20260814 `
  --out docs/data/ironclad-model-v2
```

Neuvillette（单人、A10、第四幕开启、赞助者遗物关闭）：

```powershell
dotnet run --project analysis/Sts2CharacterModel -- build `
  --source D:\Work\sts2\code `
  --mod-source D:\Work\sts2\Neuvillette\Neuvillette `
  --character Neuvillette `
  --ascension 10 `
  --seed 20260814 `
  --act4 true `
  --sponsor-relics false `
  --out docs/data/neuvillette-model-v2
```

可选参数：

- `--map-samples`：每幕地图数，默认 `100000`。
- `--reward-samples`：奖励及未知房样本流数，默认 `100000`。
- `--mod-source`：Neuvillette Mod 根目录。
- `--act4`：是否启用第四幕；本报告固定为 `true`。
- `--sponsor-relics`：是否启用赞助者遗物；本报告固定为 `false`。

退出码：`0` 表示全部验收通过，`2` 表示参数错误，`3` 表示生成完成但至少一项验收失败，`1` 表示运行异常。

输出采用固定排序、固定种子和源码最大修改时间作为生成时间；相同源码、参数和种子应产生字节一致的 CSV/JSON。
