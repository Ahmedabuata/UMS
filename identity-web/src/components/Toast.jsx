import React, { useEffect } from 'react';

export const Toast = ({ message, type = 'success', onClose }) => {
  useEffect(() => {
    const timer = setTimeout(() => {
      onClose();
    }, 3000);

    return () => clearTimeout(timer); // ✅ Gold إلغاء Gold آمن Gold
  }, [onClose]); // ✅ Gold onClose Gold مستقر Gold بسبب useCallback Gold

  const styles = {
    success: 'bg-[var(--bg-secondary)] border-green-500/20 text-green-400',
    error: 'bg-[var(--bg-secondary)] border-red-500/20 text-red-400',
  };

  const dots = {
    success: 'bg-green-400',
    error: 'bg-red-400',
  };

  return (
    <div role="alert" aria-live="assertive" className="fixed bottom-6 left-6 z-50 animate-fade-in">
      <div className={`flex items-center gap-3 px-4 py-3 rounded-xl border text-sm font-medium shadow-lg ${styles[type] || styles.success}`}>
        <span className={`w-2 h-2 rounded-full ${dots[type] || dots.success}`}></span>
        <span>{message}</span>
        <button onClick={onClose} className="ml-2 text-[var(--text-muted)] hover:text-[var(--text-main)]">✕</button>
      </div>
    </div>
  );
};

export default Toast;