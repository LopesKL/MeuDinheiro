import { DatePicker } from 'antd';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import locale from 'antd/es/date-picker/locale/pt_BR';

dayjs.locale('pt-br');

/**
 * Componente base de input de data usando Ant Design
 */
const BaseDateInput = ({ ...props }) => {
  return <DatePicker {...props} locale={locale} style={{ width: '100%' }} />;
};

export default BaseDateInput;
