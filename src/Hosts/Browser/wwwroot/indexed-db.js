const database = new Promise((resolve, reject) => {
    const request = indexedDB.open("music-player", 1);

    request.onupgradeneeded = () => {
        const db = request.result;

        for (const storeName of ["albums", "artists", "playlists", "tracks"]) {
            if (!db.objectStoreNames.contains(storeName)) {
                db.createObjectStore(storeName, { keyPath: "id" });
            }
        }
    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
});

export async function put(storeName, key, valueJson) {
    const db = await database;
    const transaction = db.transaction(storeName, "readwrite");

    transaction.objectStore(storeName).put({ id: key, value: JSON.parse(valueJson) });

    await complete(transaction);
}

export async function get(storeName, key) {
    const db = await database;
    const transaction = db.transaction(storeName, "readonly");
    const item = await requestResult(transaction.objectStore(storeName).get(key));

    return JSON.stringify(item?.value ?? null);
}

export async function getAll(storeName) {
    const db = await database;
    const transaction = db.transaction(storeName, "readonly");
    const items = await requestResult(transaction.objectStore(storeName).getAll());

    return JSON.stringify(items.map(item => item.value));
}

export async function remove(storeName, key) {
    const db = await database;
    const transaction = db.transaction(storeName, "readwrite");

    transaction.objectStore(storeName).delete(key);

    await complete(transaction);
}

function requestResult(request) {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function complete(transaction) {
    return new Promise((resolve, reject) => {
        transaction.oncomplete = resolve;
        transaction.onabort = () => reject(transaction.error);
        transaction.onerror = () => reject(transaction.error);
    });
}
