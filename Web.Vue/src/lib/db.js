const DB_NAME = 'auth';
const DB_VERSION = 1;
const STORES = ['users'];

function openDb() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VERSION);
    req.onupgradeneeded = e => {
      const db = e.target.result;
      for (const name of STORES) {
        if (!db.objectStoreNames.contains(name)) {
          db.createObjectStore(name, { keyPath: 'id' });
        }
      }
    };
    req.onsuccess = e => resolve(e.target.result);
    req.onerror = e => reject(e.target.error);
  });
}

// Called without options: returns Array | null (used for cache checks in stores).
// Called with options: returns { rows, total } after applying filter, sort, and paging.
export async function getAll(storeName, options) {
  const db = await openDb();
  const all = await new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readonly');
    const req = tx.objectStore(storeName).getAll();
    req.onsuccess = e => resolve(e.target.result);
    req.onerror = e => reject(e.target.error);
  });

  if (!options) return all.length ? all : null;

  const { filterText = '', columns = [], sortKey = null, sortDir = 'asc', page = 1, perPage = 10 } = options;

  let result = all;

  if (filterText.trim()) {
    const q = filterText.trim().toLowerCase();
    result = result.filter(row =>
      columns.some(col => {
        const val = col.sortValue ? col.sortValue(row) : (row[col.key] ?? '');
        return String(val).toLowerCase().includes(q);
      })
    );
  }

  const total = result.length;

  if (sortKey) {
    const col = columns.find(c => c.key === sortKey);
    const dir = sortDir === 'asc' ? 1 : -1;
    result = [...result].sort((a, b) => {
      const av = String(col?.sortValue ? col.sortValue(a) : (a[sortKey] ?? '')).toLowerCase();
      const bv = String(col?.sortValue ? col.sortValue(b) : (b[sortKey] ?? '')).toLowerCase();
      return av < bv ? -dir : av > bv ? dir : 0;
    });
  }

  const start = (page - 1) * perPage;
  return { rows: result.slice(start, start + perPage), total };
}

export async function saveAll(storeName, records) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readwrite');
    const store = tx.objectStore(storeName);
    store.clear();
    for (const record of records) store.put(record);
    tx.oncomplete = () => resolve();
    tx.onerror = e => reject(e.target.error);
  });
}

export async function clearStore(storeName) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readwrite');
    tx.objectStore(storeName).clear();
    tx.oncomplete = () => resolve();
    tx.onerror = e => reject(e.target.error);
  });
}
