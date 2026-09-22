import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

try {
    await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
} catch (error) {
    console.error(error);
    const message = document.createElement("p");
    message.textContent = "Não foi possível abrir o armazenamento. Feche outras abas do aplicativo e recarregue. " + error.message;
    message.setAttribute("role", "alert");
    document.body.replaceChildren(message);
}
