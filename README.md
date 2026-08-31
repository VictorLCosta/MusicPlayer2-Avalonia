# MusicPlayer2 Avalonia

Port multiplataforma do MusicPlayer2 para Avalonia, criado para o Avalonia Port Challenge.

## Estrutura

- src/MusicPlayer2-Avalonia: aplicacao compartilhada Avalonia, com UI e view models.
- src/Hosts/Desktop: host desktop para Windows, macOS e Linux.
- src/Hosts/Android, src/Hosts/iOS e src/Hosts/Browser: hosts opcionais para demonstrar o mesmo nucleo em outros destinos.
- docs/PRODUCT_SCOPE.md: escopo do port, prioridades e fronteiras entre recursos compartilhados e especificos de plataforma.

## Desenvolvimento

    dotnet build MusicPlayer2-Avalonia.slnx
    dotnet run --project src/Hosts/Desktop
