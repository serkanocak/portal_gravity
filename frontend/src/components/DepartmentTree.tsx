import React, { useState } from 'react';
import { ChevronRight, ChevronDown, Folder, Briefcase } from 'lucide-react';
import styles from './DepartmentTree.module.css';

interface Department {
  id: string;
  name: string;
  children?: Department[];
}

interface DepartmentTreeProps {
  data: Department[];
}

const TreeNode: React.FC<{ node: Department }> = ({ node }) => {
  const [isOpen, setIsOpen] = useState(false);
  const hasChildren = node.children && node.children.length > 0;

  return (
    <div className={styles.treeNode}>
      <div 
        className={`${styles.nodeContent} ${hasChildren ? styles.clickable : ''}`} 
        onClick={() => hasChildren && setIsOpen(!isOpen)}
      >
        <div className={styles.iconWrapper}>
          {hasChildren ? (
            isOpen ? <ChevronDown size={16} /> : <ChevronRight size={16} />
          ) : (
            <span style={{ width: 16 }}></span> // Spacer
          )}
        </div>
        
        {hasChildren ? (
          <Briefcase size={16} className={styles.folderIcon} />
        ) : (
          <Folder size={16} className={styles.itemIcon} />
        )}
        
        <span className={styles.nodeLabel}>{node.name}</span>
      </div>
      
      {hasChildren && isOpen && (
        <div className={styles.children}>
          {node.children!.map((child) => (
            <TreeNode key={child.id} node={child} />
          ))}
        </div>
      )}
    </div>
  );
};

export const DepartmentTree: React.FC<DepartmentTreeProps> = ({ data }) => {
  return (
    <div className={styles.treeContainer}>
      {data.map((node) => (
        <TreeNode key={node.id} node={node} />
      ))}
    </div>
  );
};
