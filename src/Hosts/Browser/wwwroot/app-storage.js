// Application files and SQLite snapshots share one durable store.
let database;
let ownsLock = false;

export async function initialize(name) {
    if (!navigator.locks || !globalThis.indexedDB) {
        throw new Error("Este navegador não oferece o armazenamento necessário. Use HTTPS ou localhost.");
    }

    // A WASM filesystem belongs to one tab. Never allow two independent copies
    // to overwrite the same persisted database. The browser releases this on exit.
    await new Promise((resolve, reject) => {
        navigator.locks.request(`musicplayer-sqlite:${name}`, { ifAvailable: true }, async lock => {
            if (!lock) {
                reject(new Error("O banco já está aberto em outra aba. Feche a outra aba e recarregue."));
                return;
            }
            ownsLock = true;
            resolve();
            await new Promise(() => {});
        }).catch(reject);
    });

    database = await new Promise((resolve, reject) => {
        const request = indexedDB.open(name, 2);
        request.onupgradeneeded = () => {
            const files = request.result.createObjectStore("files");
            // Preserve the snapshot produced by the initial SQLite integration.
            if (request.result.objectStoreNames.contains("snapshots")) {
                const oldSnapshot = request.transaction.objectStore("snapshots").get("main");
                oldSnapshot.onsuccess = () => {
                    if (oldSnapshot.result) files.put(oldSnapshot.result, "database-snapshots/musicplayer.db");
                };
            }
        };
        request.onerror = () => reject(request.error);
        request.onblocked = () => reject(new Error("Atualização do armazenamento bloqueada por outra aba."));
        request.onsuccess = () => {
            request.result.onversionchange = () => {
                request.result.close();
                database = undefined;
            };
            resolve(request.result);
        };
    });
}

export async function restore() {
    assertReady();
    const transaction = database.transaction("files", "readonly");
    const done = complete(transaction);
    const store = transaction.objectStore("files");
    const keys = store.getAllKeys();
    const values = store.getAll();
    await done;
    return JSON.stringify(Object.fromEntries(keys.result.map((key, index) => [key, values.result[index]])));
}

export async function persist(path, base64) {
    assertReady();
    const transaction = database.transaction("files", "readwrite", { durability: "strict" });
    const done = complete(transaction);
    transaction.objectStore("files").put(base64, path);
    await done;
}

export async function remove(path) {
    assertReady();
    const transaction = database.transaction("files", "readwrite", { durability: "strict" });
    const done = complete(transaction);
    transaction.objectStore("files").delete(path);
    await done;
}

function assertReady() {
    if (!database || !ownsLock) throw new Error("Armazenamento SQLite não inicializado.");
}

function complete(transaction) {
    return new Promise((resolve, reject) => {
        transaction.oncomplete = resolve;
        transaction.onabort = () => reject(transaction.error ?? new Error("Gravação cancelada."));
        transaction.onerror = () => reject(transaction.error);
    });
}
