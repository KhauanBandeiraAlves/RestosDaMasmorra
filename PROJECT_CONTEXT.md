# Restos da Masmorra — Contexto do Projeto

Este arquivo é a referência resumida para agentes (Claude, Codex, etc.) trabalharem neste
projeto de forma consistente. Leia antes de propor mudanças estruturais.

## Ficha técnica

- **Engine:** Unity 6.3 LTS (6000.3.23f1)
- **Render Pipeline:** URP (Universal Render Pipeline) 17.3.0
- **Plataforma inicial:** PC / Windows / Steam
- **Multiplayer planejado:** cooperativo, 1 a 4 jogadores, arquitetura **host-authoritative**
  (ainda não implementado)
- **Câmera:** isométrica fixa
- **Direção de arte:** 3D low-poly fantasy estilizado, referência principal **KayKit**
  (KayKit Dungeon, Adventurers, Character Animations, Resource Bits, RPG Tools Bits,
  Fantasy Weapons Bits, Skeletons; futuramente também Quaternius Fantasy Props MegaKit e
  Quaternius Medieval Village MegaKit)

## Conceito do jogo

O jogador **não** é o herói tradicional da dungeon. Aventureiros NPC entram e avançam pelas
dungeons sozinhos, lutam, quebram equipamentos e deixam restos/itens pelo caminho. O jogador
os segue coletando armas quebradas, armaduras, materiais, ossos, couro, moedas e itens
descartados — podendo eventualmente roubar loot dos próprios aventureiros.

### Loop principal

1. Jogador entra na dungeon com mochila limitada e stamina, seguindo os aventureiros.
2. Coleta restos e decide até onde avançar antes de retornar.
3. Na base: desmonta itens em matérias-primas, fabrica novos equipamentos, vende
   equipamentos aos aventureiros e equipa os próprios aventureiros contratados.
4. Equipamentos têm durabilidade; quando quebram, voltam ao ciclo de reciclagem.
5. Com progresso, o jogador contrata aventureiros, coletores e ajudantes.
6. Dungeons antigas já concluídas podem ser **automatizadas**. Automação nunca desbloqueia
   conteúdo principal novo — dungeons novas/importantes continuam exigindo exploração ativa.

### Crafting modular

Peças combináveis (ex.: lâmina + cabo + guarda = arma). O jogador pode experimentar
combinações e salvar receitas favoritas.

### Dungeons — geração híbrida (futura, não implementada ainda)

Não são mapas totalmente fixos: salas importantes, boss rooms e entrance rooms são feitas
à mão; demais salas são selecionadas proceduralmente a partir de prefabs conectados por
sockets, com geração baseada em seed, ramificações opcionais e variantes de
decoração/eventos. Arquitetura conceitual prevista: `RoomDefinition`, `RoomSocket`,
`DungeonDefinition`, `DungeonGenerator`, `DungeonBuilder`/Editor Window.

## Organização de pastas

```
Assets/
    _Project/          Todo código e conteúdo próprio do jogo
        Art/
        Audio/
        Materials/
        Prefabs/
            Characters/ Dungeon/ Items/ Environment/ UI/
        Scenes/
            Bootstrap.unity   ← cena inicial (init, save, multiplayer, scene flow no futuro)
        ScriptableObjects/
            Characters/ Dungeon/ Items/ Crafting/
        Scripts/
            Core/ Characters/ Dungeon/ Items/ Crafting/ Economy/
            Automation/ Networking/ Player/ UI/ Editor/
        Settings/       URP assets, volume profiles, input actions
        VFX/
    ThirdParty/         Somente conteúdo externo (packs de asset store, etc.)
        KayKit/
        Quaternius/
```

**Regra de ThirdParty:** conteúdo externo (KayKit, Quaternius, etc.) fica isolado em
`Assets/ThirdParty/`, nunca reorganizado ou editado diretamente. Código e assets próprios
do jogo ficam sempre em `Assets/_Project/`.

## Decisões arquiteturais conhecidas

- Namespace base para scripts próprios: `RestosDaMasmorra` (e subnamespaces por área, ex.:
  `RestosDaMasmorra.Dungeon`, `RestosDaMasmorra.Crafting`).
- Multiplayer será host-authoritative, mas a implementação ainda não começou.
- A pipeline de dungeons será híbrida (mão + procedural), mas o sistema ainda não existe no
  projeto — apenas a pasta `Scripts/Dungeon` está reservada para ele.
- Asset Serialization está como **Force Text** e Version Control como **Visible Meta
  Files** — manter assim para diffs legíveis em git.

## Não fazer sem aprovação

- Não trocar de engine.
- Não trocar de render pipeline (URP).
- Não adicionar servidor dedicado.
- Não transformar o jogador em combatente tradicional.
- Não instalar frameworks grandes sem necessidade.
- Não importar assets pagos.
- Não alterar assets de `ThirdParty` diretamente.
- Não implementar sistemas enormes antes do protótipo.
- Não criar dependências desnecessárias.
