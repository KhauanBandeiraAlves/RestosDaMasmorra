# _Project

Todo o conteúdo e código próprios de Restos da Masmorra vivem aqui. Conteúdo de terceiros
(KayKit, Quaternius, etc.) fica em `Assets/ThirdParty/` e nunca é misturado com esta pasta.

- **Art/** — arte 2D/3D própria (texturas, ícones, etc. que não sejam de packs de terceiros).
- **Audio/** — música e efeitos sonoros próprios.
- **Materials/** — materiais Unity próprios.
- **Prefabs/** — prefabs próprios, organizados por domínio: `Characters/`, `Dungeon/`,
  `Items/`, `Environment/`, `UI/`.
- **Scenes/** — cenas do jogo. `Bootstrap.unity` é a cena inicial, responsável no futuro por
  inicialização, save, multiplayer e scene flow.
- **ScriptableObjects/** — definições de dados (`Characters/`, `Dungeon/`, `Items/`,
  `Crafting/`).
- **Scripts/** — código C# próprio, sob o namespace `RestosDaMasmorra` (e subnamespaces por
  pasta): `Core/`, `Characters/`, `Dungeon/`, `Items/`, `Crafting/`, `Economy/`,
  `Automation/`, `Networking/`, `Player/`, `UI/`, `Editor/`.
- **Settings/** — assets de configuração do projeto (URP Renderer/Pipeline Assets, Volume
  Profiles, Input Actions).
- **VFX/** — efeitos visuais próprios.

Veja [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md) na raiz do repositório para o contexto
completo do jogo e as regras arquiteturais.
