import { Result, Button } from 'antd';
import { HomeOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';

const NotFound = () => {
  const navigate = useNavigate();

  return (
    <Result
      status="404"
      title="404"
      subTitle="Desculpe, a página que você está procurando não existe."
      extra={
        <Button type="primary" icon={<HomeOutlined />} onClick={() => navigate('/')}>
          Voltar para Home
        </Button>
      }
    />
  );
};

export default NotFound;
