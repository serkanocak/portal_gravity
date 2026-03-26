import React from 'react';
import { useLocaleStore } from '../store/localeStore';
import { Globe } from 'lucide-react';
import styles from './shared.module.css';

export const LanguageSelector: React.FC = () => {
  const { locale, setLocale } = useLocaleStore();

  const handleSelect = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setLocale(e.target.value);
  };

  return (
    <div className={styles.languageSelector}>
      <Globe size={16} />
      <select value={locale} onChange={handleSelect}>
        <option value="en">English (US)</option>
        <option value="tr">Türkçe (TR)</option>
      </select>
    </div>
  );
};
