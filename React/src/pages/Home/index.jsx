import { Row, Col } from 'antd';
import { Card } from '@components/Layout';
import { useNavigate } from 'react-router-dom';
import { HomeOutlined, FormOutlined, TableOutlined, FileTextOutlined, BarChartOutlined, BuildOutlined } from '@ant-design/icons';

const Home = () => {
  const navigate = useNavigate();

  const cards = [
    { icon: <FormOutlined />, title: 'Formulário', description: 'Exemplo de formulário dinâmico', path: '/form' },
    { icon: <TableOutlined />, title: 'CRUD', description: 'Exemplo de tabela paginada', path: '/crud' },
    { icon: <FileTextOutlined />, title: 'Modal', description: 'Exemplo de modal', path: '/modal' },
    { icon: <BarChartOutlined />, title: 'Gráficos', description: 'Exemplo de gráficos', path: '/charts' },
    { icon: <BuildOutlined />, title: 'Form Builder', description: 'Construtor de formulários', path: '/formBuilder' },
  ];

  return (
    <div>
      <h1>Bem-vindo ao Framework React</h1>
      <Row gutter={[16, 16]}>
        {cards.map((card, index) => (
          <Col key={index} xs={24} sm={12} md={8} lg={8} xl={8}>
            <Card
              hoverable
              icon={card.icon}
              title={card.title}
              description={card.description}
              onClick={() => navigate(card.path)}
              style={{ cursor: 'pointer', height: '100%' }}
            />
          </Col>
        ))}
      </Row>
    </div>
  );
};

export default Home;
