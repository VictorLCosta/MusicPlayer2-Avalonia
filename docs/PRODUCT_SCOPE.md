# Escopo do port

O MusicPlayer2 original e um reprodutor local Windows/MFC. Este port preserva o fluxo que torna o produto util no dia a dia, mas adapta as integracoes para uma arquitetura multiplataforma.

## Entrega inicial

1. **Reproducao local e fila**
   - Abrir arquivos e pastas, ler metadados, reproduzir, pausar, parar, buscar, avancar e voltar.
   - Modos de repeticao e aleatorio, com fila persistida entre sessoes.
2. **Biblioteca**
   - Indexar pastas configuradas, pesquisar, filtrar e navegar por artista, album e musica.
   - Exibir duracao, artista, album, capa e estado de reproducao.
3. **Experiencia de player**
   - Tela principal responsiva, mini player, atalhos de teclado e controles de midia quando a plataforma oferecer suporte.
   - Tema claro/escuro e configuracao basica persistida.
4. **Letras e capa**
   - Localizar arquivos LRC locais, sincronizar a linha atual e apresentar a capa incorporada.
   - Downloads e edicao ficam para uma etapa posterior, pois dependem de servicos externos e licencas.
5. **Polimento para a competicao**
   - Visualizador de espectro, telas de comparacao antes/depois, builds desktop para Windows, macOS e Linux e relato de migracao.

## Recursos posteriores

- Edicao de tags, editor de letras, importacao CUE e conversao de formatos.
- Equalizador, reverberacao e efeitos avancados.
- Letras na area de trabalho, controles de miniatura da barra de tarefas e integracoes exclusivas do Windows.
- Busca e download online de capas e letras, apos definir provedores e requisitos de uso.

## Organizacao do codigo

O codigo compartilhado vive em src/MusicPlayer2-Avalonia. A medida que cada recurso for implementado, ele segue esta organizacao:

    src/MusicPlayer2-Avalonia/
      Features/
        Playback/       estado, comandos e UI de reproducao
        Library/        indexacao, consultas e listagens
        Queue/          fila, historico e modos de reproducao
        Lyrics/         parser LRC e sincronizacao
        Settings/       preferencias persistidas
      Services/
        Audio/          contrato e implementacoes por plataforma
        Metadata/       leitura de tags e capas
        Storage/        configuracao e dados locais
      Views/
      ViewModels/
      Assets/

Os projetos fora de src sao apenas hosts de plataforma. Eles nao devem conter regra de negocio; oferecem implementacoes para os contratos em Services quando uma capacidade depender do sistema operacional.

## Criterio de demonstracao

O primeiro marco esta pronto quando uma pessoa pode escolher uma pasta de musicas, navegar pela biblioteca criada, iniciar uma faixa, controlar a fila e fechar/reabrir o aplicativo sem perder a configuracao. Letras sincronizadas e o visualizador tornam a demonstracao mais proxima do MusicPlayer2 original, sem bloquear esse fluxo principal.
