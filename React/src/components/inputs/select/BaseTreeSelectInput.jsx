import { TreeSelect } from 'antd';

/**
 * Componente base de tree select usando Ant Design
 */
const BaseTreeSelectInput = ({ ...props }) => {
  return <TreeSelect {...props} style={{ width: '100%' }} />;
};

export default BaseTreeSelectInput;
