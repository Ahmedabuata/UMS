import React, { createContext, useContext, useState, useEffect, useRef } from 'react';
import i18n from '../i18n';

const LanguageContext = createContext(null);

export const LanguageProvider = ({ children }) => {
  const [language, setLanguage] = useState(() => localStorage.getItem('app_lang') || 'ar');
  const mounted = useRef(false);

  useEffect(() => {
    if (mounted.current && i18n.language?.startsWith(language)) return;
    mounted.current = true;
    i18n.changeLanguage(language);
    localStorage.setItem('app_lang', language);
    localStorage.setItem('i18nextLng', language);
    document.documentElement.lang = language;
    document.documentElement.dir = language === 'ar'? 'rtl' : 'ltr';
  }, [language]);

  const changeLanguage = (l) => { if (l!== language) setLanguage(l); };
  const toggleLanguage = () => setLanguage(p => p === 'ar'? 'en' : 'ar');

  return (
    <LanguageContext.Provider value={{ language, changeLanguage, toggleLanguage, isRTL: language === 'ar' }}>
      {children}
    </LanguageContext.Provider>
  );
};

export const useLanguage = () => {
  const c = useContext(LanguageContext);
  if (!c) throw new Error('useLanguage must be inside LanguageProvider');
  return c;
};
