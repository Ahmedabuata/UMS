import React from 'react';
import { Link } from 'react-router-dom';
import { useLanguage } from '../context/LanguageContext';

export const Unauthorized = () => {
  const { t } = useLanguage();

  return (
    <div className="p-6 flex flex-col items-center justify-center min-h-[70vh] text-[var(--text-main)]">
      <div className="p-8 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl text-center max-w-md w-full space-y-4 shadow-sm">
        <h1 className="text-6xl font-bold text-red-500">403</h1>
        <h2 className="text-2xl font-bold text-[var(--text-main)]">{t('common.unauthorized')}</h2>
        <p className="text-[var(--text-muted)] text-sm">
          {t('common.noPermissionDetail')}
        </p>
        <div className="pt-2">
          <Link
            to="/dashboard"
            className="inline-block px-6 py-2.5 bg-[var(--accent-color)] text-white rounded-xl hover:opacity-90 transition-opacity text-sm font-medium"
          >
            {t('common.backToDashboard')}
          </Link>
        </div>
      </div>
    </div>
  );
};

export default Unauthorized;
