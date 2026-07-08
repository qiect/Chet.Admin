import { ref } from 'vue';

import { requestClient } from '#/api/request';

const dictCache = new Map<string, { label: string; value: string }[]>();

export function useDict(code: string) {
  const options = ref<{ label: string; value: string }[]>([]);
  const loading = ref(false);

  async function load() {
    if (dictCache.has(code)) {
      options.value = dictCache.get(code)!;
      return;
    }
    loading.value = true;
    try {
      const res = await requestClient.get(`/dictionaries/code/${code}`);
      const items = res?.items || res || [];
      const mapped = items.map((item: any) => ({
        label: item.label,
        value: item.value,
      }));
      dictCache.set(code, mapped);
      options.value = mapped;
    } catch {
      options.value = [];
    } finally {
      loading.value = false;
    }
  }

  load();

  return { options, loading, refresh: load };
}
