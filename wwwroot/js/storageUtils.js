window.blazorwpStorage = {
  keys: () => Object.keys(window.localStorage),
  itemInfo: key => {
    const value = window.localStorage.getItem(key);
    let ts = window.localStorage.getItem(key + '_timestamp');
    if (!ts && value) {
      try {
        const obj = JSON.parse(value);
        if (obj && typeof obj === 'object' && obj.lastUpdated) {
          ts = obj.lastUpdated;
        }
      } catch { }
    }
    return { value: value, lastUpdated: ts };
  },
  delete: key => {
    window.localStorage.removeItem(key);
    window.localStorage.removeItem(key + '_timestamp');
  }
};
