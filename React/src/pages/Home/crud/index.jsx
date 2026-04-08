import { Card } from '@components/Layout';
import { PaginatedTable } from '@components/Data';
import { Button, Space } from 'antd';
import { EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { useRef } from 'react';

const CrudPage = () => {
  const tableRef = useRef();

  // Exemplo de função de busca
  const fetchData = async (page, pageSize, sorterField, sortOrder) => {
    // Simular chamada à API
    await new Promise((resolve) => setTimeout(resolve, 500));

    const mockData = Array.from({ length: 50 }, (_, i) => ({
      id: i + 1,
      nome: `Item ${i + 1}`,
      email: `item${i + 1}@example.com`,
      status: i % 2 === 0 ? 'Ativo' : 'Inativo',
    }));

    // Aplicar ordenação
    let sortedData = [...mockData];
    if (sorterField) {
      sortedData.sort((a, b) => {
        const aVal = a[sorterField];
        const bVal = b[sorterField];
        if (sortOrder === 'asc') {
          return aVal > bVal ? 1 : -1;
        } else {
          return aVal < bVal ? 1 : -1;
        }
      });
    }

    // Aplicar paginação
    const start = (page - 1) * pageSize;
    const end = start + pageSize;
    const paginatedData = sortedData.slice(start, end);

    return {
      data: paginatedData,
      total: mockData.length,
    };
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      sorter: true,
    },
    {
      title: 'Nome',
      dataIndex: 'nome',
      key: 'nome',
      sorter: true,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      sorter: true,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
    },
  ];

  const actions = [
    {
      label: 'Editar',
      icon: <EditOutlined />,
      onClick: (record) => {
        console.log('Editar:', record);
      },
    },
    {
      label: 'Excluir',
      icon: <DeleteOutlined />,
      danger: true,
      onClick: (record) => {
        console.log('Excluir:', record);
      },
    },
  ];

  return (
    <Card
      title="Exemplo de CRUD"
      extra={
        <Button type="primary" onClick={() => tableRef.current?.reload()}>
          Recarregar
        </Button>
      }
    >
      <PaginatedTable
        ref={tableRef}
        fetchData={fetchData}
        columns={columns}
        actions={actions}
        initialPageSize={10}
      />
    </Card>
  );
};

export default CrudPage;
