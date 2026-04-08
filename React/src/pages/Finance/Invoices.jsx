import { useEffect, useState } from 'react';
import { Card, Select, DatePicker, Table, Typography, Statistic, Row, Col } from 'antd';
import dayjs from 'dayjs';
import { financeApi } from '@services/financeApi';
import { App } from 'antd';

const { Title } = Typography;

const formatMoney = (v) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(v || 0));

const Invoices = () => {
  const { message } = App.useApp();
  const [cards, setCards] = useState([]);
  const [cardId, setCardId] = useState(null);
  const [month, setMonth] = useState(dayjs());
  const [invoice, setInvoice] = useState(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const c = await financeApi.creditCards();
        setCards(c || []);
        if (c?.length) setCardId(c[0].id);
      } catch (e) {
        message.error(e.message || 'Erro ao carregar cartões');
      }
    })();
  }, [message]);

  const loadInvoice = async () => {
    if (!cardId) return;
    setLoading(true);
    try {
      const y = month.year();
      const m = month.month() + 1;
      const inv = await financeApi.invoice(cardId, y, m);
      setInvoice(inv);
    } catch (e) {
      message.error(e.message || 'Erro ao carregar fatura');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadInvoice();
  }, [cardId, month]);

  const columns = [
    { title: 'Data', dataIndex: 'date', key: 'date', render: (d) => dayjs(d).format('DD/MM/YYYY') },
    { title: 'Descrição', dataIndex: 'description', key: 'description' },
    { title: 'Categoria', dataIndex: 'categoryName', key: 'categoryName' },
    { title: 'Valor', dataIndex: 'amount', key: 'amount', render: (v) => formatMoney(v) },
  ];

  return (
    <div>
      <Title level={3} style={{ marginTop: 0 }}>
        Faturas por cartão
      </Title>
      <Card>
        <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
          <Col xs={24} md={12}>
            <Select
              style={{ width: '100%' }}
              placeholder="Cartão"
              value={cardId}
              onChange={setCardId}
              options={cards.map((c) => ({ value: c.id, label: c.name }))}
            />
          </Col>
          <Col xs={24} md={12}>
            <DatePicker picker="month" style={{ width: '100%' }} value={month} onChange={(v) => v && setMonth(v)} />
          </Col>
        </Row>
        <Statistic title="Total no período" value={invoice?.total ?? 0} formatter={formatMoney} />
        <Table
          style={{ marginTop: 16 }}
          rowKey="id"
          loading={loading}
          dataSource={invoice?.expenses || []}
          columns={columns}
          pagination={{ pageSize: 8 }}
          scroll={{ x: true }}
        />
      </Card>
    </div>
  );
};

export default Invoices;
