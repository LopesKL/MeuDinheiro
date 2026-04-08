import { Select } from 'antd';

/**
 * Componente base de select usando Ant Design
 */
const BaseSelectInput = ({ ...props }) => {
  return <Select {...props} style={{ width: '100%' }} />;
};

export default BaseSelectInput;
