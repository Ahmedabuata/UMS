import { useState, useCallback } from 'react';

export const useToast = () => {
  const [toast, setToast] = useState(null);

  const showToast = useCallback((message, type = 'success') => {
    setToast({ message, type });
  }, []);

  const hideToast = useCallback(() => {
    setToast(null);
  }, []);

  // ✅ Gold API Gold ثابت Gold - نفس المشروع Gold - لا setToast Gold في return Gold
  return { toast, showToast, hideToast };
};

export default useToast;