import React, { useState } from 'react';
import { HelpCircle, X } from 'lucide-react';
import DOMPurify from 'dompurify';
import { useHelpGuide } from '../shared/hooks/useHelpGuide';
import styles from './shared.module.css';

interface HelpPopupProps {
  slug: string;
}

export const HelpPopup: React.FC<HelpPopupProps> = ({ slug }) => {
  const [isOpen, setIsOpen] = useState(false);
  const { data: guide, isLoading } = useHelpGuide(slug);

  return (
    <div className={styles.helpContainer}>
      <button 
        className={styles.helpTrigger} 
        onClick={() => setIsOpen(!isOpen)}
        aria-label="Help"
      >
        <HelpCircle size={20} />
      </button>

      {isOpen && (
        <div className={styles.helpPopup}>
          <div className={styles.helpHeader}>
            <h3>{isLoading ? 'Loading...' : guide?.title || 'Help'}</h3>
            <button className={styles.closeBtn} onClick={() => setIsOpen(false)}>
              <X size={16} />
            </button>
          </div>
          <div className={styles.helpBody}>
            {isLoading ? (
              <div className={styles.loadingSpinner}></div>
            ) : guide?.contentHtml ? (
              <div 
                dangerouslySetInnerHTML={{ 
                  __html: DOMPurify.sanitize(guide.contentHtml) 
                }} 
              />
            ) : (
              <p>No content available for this section.</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
